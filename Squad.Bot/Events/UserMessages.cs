﻿using Discord.WebSocket;
using Squad.Bot.Data;
using Squad.Bot.Logging;

namespace Squad.Bot.Events
{
    public class UserMessages
    {

        private readonly SquadDBContext _dbContext;
        private readonly Logger _logger;

        public UserMessages(SquadDBContext dbContext, Logger logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        // TODO: Change the stubs with the working code
        public async Task OnUserMessageReceived(SocketMessage message)
        {
            _logger.LogDebug("{nameOfFunc} has been executed by {Author.Username}:{Author.Id} in {Channel.Id} channel", nameof(OnUserMessageReceived), message.Author.Username, message.Author.Id, message.Channel.Id);
        }
    }
}
