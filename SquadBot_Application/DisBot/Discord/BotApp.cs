﻿using Discord.WebSocket;
using Discord;
using Squad.Bot.Models;
using Discord.Interactions;
using Microsoft.EntityFrameworkCore;
using Squad.Bot.DisBot.Data;
using Squad.Bot.DisBot.DisLogging;
using Squad.Bot.DisBot.DsEvents;

namespace Squad.Bot.DisBot.Discord
{
    internal class BotApp
    {
        private readonly IServiceProvider _services;
        private readonly IConfiguration _configuration;
        private readonly Config? _config;

        private readonly DiscordSocketConfig _socketConfig = new()
        {
            GatewayIntents = GatewayIntents.All,
            // Download users so that all users are available in large guilds
            AlwaysDownloadUsers = true
        };

        public BotApp(Config? config)
        {
            _config = config;

            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            _socketConfig.TotalShards = _config.TotalShards;

            var options = new DbContextOptionsBuilder<SquadDBContext>()
                .UseSqlite(config.DbOptions)
                .Options;

            // Add services to dependency injection
            _services = new ServiceCollection()
                .AddSingleton(_socketConfig)
                .AddSingleton(_configuration)
                .AddSingleton(new SquadDBContext(options))
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                .AddSingleton<InteractionHandler>()
                .BuildServiceProvider();
        }

        internal static bool IsDebug()
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }

        internal async Task<Exception?> RunAsync()
        {
            try
            {
                var _discordClient = _services.GetRequiredService<DiscordSocketClient>();

                await _services.GetRequiredService<InteractionHandler>()
                .InitializeAsync();

                _discordClient.UserLeft += UserGuildEvent.OnUserLeftGuild;
                _discordClient.UserJoined += UserGuildEvent.OnUserJoinGuild;
                _discordClient.MessageReceived += UserMessages.OnUserMessageReceived;
                _discordClient.UserVoiceStateUpdated += OnUserStateChange.OnUserVoiceStateUpdate;

                // Login and start bot
                await _discordClient.LoginAsync(TokenType.Bot, _config.Token);
                await _discordClient.StartAsync();

                DisLogger.LogInfo("Bot has started");

                // Block the task indefinitely
                await Task.Delay(Timeout.Infinite);
            }
            catch (Exception e)
            {
                return e;
            }

            return null;
        }
    }
}
