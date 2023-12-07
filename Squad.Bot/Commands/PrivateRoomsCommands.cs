﻿using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;
using Squad.Bot.Data;
using Squad.Bot.Logging;
using Squad.Bot.Utilities;
using System.Threading.Tasks;

namespace Squad.Bot.Commands
{
    [Group("private_rooms", "Help you manage private rooms")]
    public class PrivateRoomsCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly SquadDBContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrivateRoomsCommands"/> class.
        /// </summary>
        /// <param name="context">The interaction context.</param>
        public PrivateRoomsCommands(SquadDBContext context)
        {
            _dbContext = context;
        }


        /// <summary>
        /// Create a portal for the private rooms
        /// </summary>
        /// <param name="voiceChannelName">The name of the voice channel.</param>
        /// <param name="settingsChannelName">The name of the settings channel.</param>
        /// <param name="categoryName">The name of the category.</param>
        [SlashCommand("invite", "Create a portal for the private rooms")]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        public async Task Invite(string voiceChannelName = "[➕] Create", string settingsChannelName = "[⚙️] Settings", string categoryName = "Portal")
        {
            var savedPortal = await _dbContext.PrivateRooms.FirstOrDefaultAsync(x => x.Guilds.Id == Context.Guild.Id);
            N:

            if (savedPortal?.CategoryID != null && savedPortal?.ChannelID != null)
            {
                if(Context.Guild.GetCategoryChannel(savedPortal.CategoryID) == null && Context.Guild.GetVoiceChannel(savedPortal.ChannelID) == null && Context.Guild.GetTextChannel(savedPortal.SettingsChannelID) == null)
                {
                    _dbContext.PrivateRooms.Remove(savedPortal);
                    await _dbContext.SaveChangesAsync();
                    // WARNING: goto may cause many problems in future
                    goto N;
                }
                else
                {
                    var component = new ComponentBuilder()
                                            .WithButton(label: "Delete", customId: "portal:delete", style: ButtonStyle.Danger);

                    await RespondAsync(text: $"{Context.User.Username}, private rooms already created", 
                                                                         components: component.Build(), 
                                                                         ephemeral: true);
                }
            }
            else
            {
                // TODO: Specify the permissions overwrites
                var overwrites = new PermissionOverwriteHelper(Context.Guild.Roles.First(x => x.Name == "@everyone").Id, PermissionTarget.Role)
                {
                    Permissions = PermissionOverwriteHelper.SetOverwritePermissions()
                };

                var category = await Context.Guild.CreateCategoryChannelAsync(categoryName, tcp => { tcp.PermissionOverwrites = overwrites.CreateOverwrites(); });

                var voiceChannel = await Context.Guild.CreateVoiceChannelAsync(voiceChannelName, tcp => {tcp.CategoryId = category.Id;
                                                                                                         tcp.PermissionOverwrites = overwrites.CreateOverwrites();});

                var settingsChannel = await Context.Guild.CreateTextChannelAsync(settingsChannelName, tcp => {tcp.CategoryId = category.Id;
                                                                                                          tcp.Topic = "manage private rooms";
                                                                                                          tcp.PermissionOverwrites = overwrites.CreateOverwrites();});

                //Buttons
                var rename = new ButtonBuilder().WithCustomId("portal:rename").WithLabel("✏️").WithStyle(ButtonStyle.Secondary);
                var hide = new ButtonBuilder().WithCustomId("portal:hide").WithLabel("🔒").WithStyle(ButtonStyle.Secondary);
                var limit = new ButtonBuilder().WithCustomId("portal:limit").WithLabel("🫂").WithStyle(ButtonStyle.Secondary);
                var kick = new ButtonBuilder().WithCustomId("portal:kick").WithLabel("🚫").WithStyle(ButtonStyle.Secondary);
                var owner = new ButtonBuilder().WithCustomId("portal:owner").WithLabel("👤").WithStyle(ButtonStyle.Secondary);

                //Component with buttons
                var components = new ComponentBuilder().WithButton(rename).WithButton(hide).WithButton(owner).WithButton(limit).WithButton(kick);

                //Final embed
                var embed = new EmbedBuilder()
                {
                    Description = "You can change the configuration of your room using interactions." +
                                  "\n" +
                                  "\n✏️ — change the name of the room" +
                                  "\n🔒 — hide/show the room" +
                                  "\n🫂 — change the user limit" +
                                  "\n🚫 — kick the participant out of the room" +
                                  "\n👤 — change the owner of the room",
                    Color = CustomColors.Default,
                }.WithAuthor(name: "Private room management", iconUrl: "https://cdn.discordapp.com/emojis/963689541724688404.webp?size=128&quality=lossless");

                await settingsChannel.SendMessageAsync(embed: embed.Build(), components: components.Build());

                var successEmbed = new EmbedBuilder
                {
                    Title = $"✅ Created private rooms.",
                    Color = CustomColors.Success,
                }.WithAuthor(name: Context.Guild.CurrentUser.Nickname, iconUrl: Context.Guild.CurrentUser.GetGuildAvatarUrl());

                await RespondAsync(embed: successEmbed.Build());
            }
        }

        // TODO: Change the stubs with the working code
        [SlashCommand("rename", "Change the name of the room")]
        public async Task Rename(string channelName)
        {
            var savedPortal = await _dbContext.PrivateRooms.FirstOrDefaultAsync(x => x.Guilds.Id == Context.Guild.Id);

            if(Context.Channel.Id == savedPortal.SettingsChannelID && Context.Guild.CurrentUser.VoiceChannel.CategoryId == savedPortal.CategoryID)
            {
                // TODO: add user owner check
                await Context.Guild.CurrentUser.VoiceChannel.ModifyAsync(x => x.Name = channelName);

                var embed = new EmbedBuilder
                {
                    Title = "Channel was successfully renamed",
                    Color = CustomColors.Success,
                };

                await RespondAsync(embed: embed.Build(), ephemeral: true);
            }
            else
            {
                var embed = new EmbedBuilder
                {
                    Title = "Something went wrong...",
                    Description = "This could be due to the fact that you were not writing in the portal settings channel or you are not in a private room",
                    Color = CustomColors.Failure,
                };
            }
        }

        [SlashCommand("hide", "Hide/show the room")]
        public async Task Hide()
        {
            await Logger.LogInfo("hide");
            var savedPortal = await _dbContext.PrivateRooms.FirstOrDefaultAsync(x => x.Guilds.Id == Context.Guild.Id);

            if (Context.Channel.Id == savedPortal.SettingsChannelID && Context.Guild.CurrentUser.VoiceChannel.CategoryId == savedPortal.CategoryID)
            {

            }
        }

        [SlashCommand("kick", "Kick the participant out of the room")]
        public async Task Kick(IUser user)
        {
            await Logger.LogInfo($"kick {user}");
            var savedPortal = await _dbContext.PrivateRooms.FirstOrDefaultAsync(x => x.Guilds.Id == Context.Guild.Id);

            if (Context.Channel.Id == savedPortal.SettingsChannelID && Context.Guild.CurrentUser.VoiceChannel.CategoryId == savedPortal.CategoryID)
            {

            }
        }

        [SlashCommand("limit", "Change the user limit")]
        public async Task Limit(ushort limit = 5)
        {
            await Logger.LogInfo($"limit {limit}");
            var savedPortal = await _dbContext.PrivateRooms.FirstOrDefaultAsync(x => x.Guilds.Id == Context.Guild.Id);

            if (Context.Channel.Id == savedPortal.SettingsChannelID && Context.Guild.CurrentUser.VoiceChannel.CategoryId == savedPortal.CategoryID)
            {

            }
        }

        [SlashCommand("owner", "Change the owner of the room")]
        public async Task Owner(IUser newOwner)
        {
            await Logger.LogInfo($"owner {newOwner}");
            var savedPortal = await _dbContext.PrivateRooms.FirstOrDefaultAsync(x => x.Guilds.Id == Context.Guild.Id);

            if (Context.Channel.Id == savedPortal.SettingsChannelID && Context.Guild.CurrentUser.VoiceChannel.CategoryId == savedPortal.CategoryID)
            {

            }
        }
    }
}
