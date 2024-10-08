﻿using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Squad.Bot.Data;
using Squad.Bot.FunctionalModules.Events;
using Squad.Bot.Logging;
using System.Reflection;
using IResult = Discord.Interactions.IResult;

namespace Squad.Bot.Discord
{
    /// <summary>
    /// This class is responsible for handling interactions with the Discord server.
    /// It uses the InteractionService to register modules that contain commands,
    /// and the DiscordSocketClient to listen for interactions.
    /// </summary>
    public class InteractionHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _handler;
        private readonly IServiceProvider _services;
        private readonly Logger _logger;

        /// <summary>
        /// Constructs a new instance of the InteractionHandler class.
        /// </summary>
        /// <param name="client">The DiscordSocketClient used to listen for interactions.</param>
        /// <param name="handler">The InteractionService used to register modules.</param>
        /// <param name="services">The service provider used to resolve dependencies.</param>
        /// <param name="configuration">The configuration used to read bot settings.</param>
        public InteractionHandler(DiscordSocketClient client, InteractionService handler, IServiceProvider services, Logger logger)
        {
            _client = client;
            _handler = handler;
            _services = services;
            _logger = logger;
        }

        /// <summary>
        /// Initializes the InteractionHandler by registering modules and subscribing to events.
        /// </summary>
        public async Task InitializeAsync()
        {

            #region service providers
            // Initialize the handler and services for communication with the server
            UserGuildEvent userGuildEvent = new(_services.GetRequiredService<SquadDBContext>(), _services.GetRequiredService<Logger>());
            OnUserStateChange userStateChange = new(_services.GetRequiredService<SquadDBContext>(), _services.GetRequiredService<Logger>());
            GuildEvent guild = new(_services.GetRequiredService<SquadDBContext>(), _services.GetRequiredService<Logger>());
            #endregion

            // Add the public modules that inherit InteractionModuleBase<T> to the InteractionService
            await _handler.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            #region Interaction payloads
            // Process the InteractionCreated payloads to execute Interactions commands
            _client.InteractionCreated += HandleInteraction;
            _handler.InteractionExecuted += InteractionExecuted;
            _client.Ready += ReadyAsync;
            #endregion

            #region events
            // Subscribes for events
            _client.UserVoiceStateUpdated += userStateChange.PrivateRooms;
            _client.UserVoiceStateUpdated += userStateChange.CollectTalkTimeData;
            _client.MessageReceived += userGuildEvent.OnUserMessageReceived;
            _client.UserLeft += userGuildEvent.OnUserLeftGuild;
            _client.UserJoined += userGuildEvent.OnUserJoinGuild;
            _client.JoinedGuild += guild.OnGuildJoined;
            _client.LeftGuild += guild.OnGuildLeft;
            #endregion
        }

        private async Task ReadyAsync()
        {
            // Context & Slash commands can be automatically registered, but this process needs to happen after the client enters the READY state.
            // Since Global Commands take around 1 hour to register, should use a test guild to instantly update and test our commands.
#if DEBUG
            await _handler.RegisterCommandsToGuildAsync(Convert.ToUInt64(_configuration["BotSettings:testGuild"]), true);
#else            
            await _handler.RegisterCommandsGloballyAsync(true);
#endif
        }

        private async Task<Task> InteractionExecuted(ICommandInfo arg1, IInteractionContext arg2, IResult arg3)
        {
            if (!arg3.IsSuccess)
            {
                switch (arg3.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        await arg2.Interaction.RespondAsync($"Unmet Precondition: {arg3.ErrorReason}", ephemeral: true);
                        break;
                    case InteractionCommandError.UnknownCommand:
                        await arg2.Interaction.RespondAsync("Unknown command", ephemeral: true);
                        break;
                    case InteractionCommandError.BadArgs:
                        await arg2.Interaction.RespondAsync("Invalid number or arguments", ephemeral: true);
                        break;
                    case InteractionCommandError.Exception:
                        var embed = new EmbedBuilder().AddField("Error", "Command exception");
                        await arg2.Interaction.RespondWithFileAsync(filePath: "/app/squad.bot.log",embed: embed.Build(), ephemeral: true);
                        break;
                    case InteractionCommandError.Unsuccessful:
                        await arg2.Interaction.RespondAsync("Command could not be executed", ephemeral: true);
                        break;
                    default:
                        break;
                }
            }
            return Task.CompletedTask;
        }

        private async Task HandleInteraction(SocketInteraction arg)
        {
            try
            {
                // Create an execution context that matches the generic type parameter of InteractionModuleBase<T> modules
                var ctx = new SocketInteractionContext(_client, arg);
                await _handler.ExecuteCommandAsync(ctx, _services);
            }
            catch (Exception ex)
            {
                _logger.LogError(message: ex.Message, ex: ex);
#pragma warning disable CS8604 // Possible null reference argument.
                Console.WriteLine(ex.StackTrace, ex.Source);
#pragma warning restore CS8604 // Possible null reference argument.

                // If a Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
                // response, or at least let the user know that something went wrong during the command execution.
                if (arg.Type == InteractionType.ApplicationCommand)
                    await arg.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
        }
    }
}