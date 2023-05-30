﻿using Discord;
using SquadBot.Discord;
using SquadBot.Utilities;
using SquadBot.Logging;
using SquadBot.Models;

namespace SquadBot
{
    public class Program
    {
        public static void Main()
        {
            Config? config = (Config?)ConfigService.GetConfig(ConfigService.ConfigType.Config);

            try
            {
                TokenUtils.ValidateToken(TokenType.Bot, config.Token);
            }
            catch
            {
                Logger.LogError("The discord bot token was invalid, please check the value :" + config.Token);
                ApplicationHelper.AnnounceAndExit();
            }

            var bot = new Bot(config.Token, config);

            // Start the bot in async context from a sync context
            var closingException = bot.RunAsync().GetAwaiter().GetResult();

            if (closingException == null)
            {
                ApplicationHelper.AnnounceAndExit();
            }
            else
            {
                Logger.LogError("Caught crashing exception");
                Logger.LogException(closingException);
                Console.WriteLine();
                ApplicationHelper.AnnounceAndExit();
            }
        }
    }
}