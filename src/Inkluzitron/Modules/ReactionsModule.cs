using Discord;
using Discord.WebSocket;
using Inkluzitron.Contracts;
using Inkluzitron.Models.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Inkluzitron.Modules
{
    public class ReactionsModule
    {
        protected DiscordSocketClient DiscordClient { get; }
        protected ReactionSettings Settings { get; }
        public ILogger<ReactionsModule> Logger { get; }
        protected IReactionHandler[] ReactionHandlers { get; }

        public ReactionsModule(DiscordSocketClient discordClient, ReactionSettings reactionSettings, IEnumerable<IReactionHandler> reactionHandlers, ILogger<ReactionsModule> logger)
        {
            DiscordClient = discordClient;
            Settings = reactionSettings;
            Logger = logger;
            ReactionHandlers = reactionHandlers.ToArray();

            DiscordClient.ReactionAdded += DiscordClient_ReactionAdded;
        }

        private async Task DiscordClient_ReactionAdded(Cacheable<IUserMessage, ulong> userMessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            var message = await userMessage.GetOrDownloadAsync();
            if (message == null) return;

            var user = reaction.User.IsSpecified ? reaction.User.Value : await DiscordClient.Rest.GetUserAsync(reaction.UserId);
            if (user == null) return;

            var ownUser = DiscordClient.CurrentUser;
            var ownId = ownUser.Id;
            if (user.Id == ownId || message.Author.Id != ownId)
                return;

            foreach (var reactionHandler in ReactionHandlers)
            {
                try
                {
                    var reactionHandled = await reactionHandler.Handle(message, reaction.Emote, user, ownUser);
                    if (reactionHandled)
                        break;
                }
                catch (Exception e)
                {
                    Logger.LogError(
                        e,
                        "Reaction handler {0} threw an exception when handling reaction {1} added to message {2}.",
                        reactionHandler,
                        reaction.Emote.Name,
                        message.Id
                    );
                }
            }
        }
    }
}
