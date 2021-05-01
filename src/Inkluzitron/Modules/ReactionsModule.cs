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

        private async Task DiscordClient_ReactionAdded(Cacheable<IUserMessage, ulong> cacheableUser, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (reaction.Message.GetValueOrDefault() is not IUserMessage message)
            {
                try
                {
                    message = (await channel.GetMessageAsync(reaction.MessageId)) as IUserMessage;
                    if (message is null)
                        return;
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Could not retrieve non-cached message that has been reacted to.");
                    return;
                }
            }

            if (reaction.User.GetValueOrDefault() is not IUser user)
            {
                try
                {
                    user = await DiscordClient.Rest.GetUserAsync(reaction.UserId);
                    if (user is null)
                        return;
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Could not retrieve non-cached user that added reaction to a message.");
                    return;
                }
            }

            var ownId = DiscordClient.CurrentUser.Id;
            if (user.Id == ownId)
                return;

            if (message.Author.Id != ownId)
                return;

            foreach (var reactionHandler in ReactionHandlers)
            {
                try
                {
                    var reactionHandled = await reactionHandler.Handle(message, reaction.Emote, user, DiscordClient.CurrentUser);
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
