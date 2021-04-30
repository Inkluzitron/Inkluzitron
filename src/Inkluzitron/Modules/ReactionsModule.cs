using Discord;
using Discord.WebSocket;
using Inkluzitron.Contracts;
using Inkluzitron.Extensions;
using Inkluzitron.Settings;
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
            DiscordClient = discordClient ?? throw new ArgumentNullException(nameof(discordClient));
            Settings = reactionSettings ?? throw new ArgumentNullException(nameof(reactionSettings));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            ReactionHandlers = (reactionHandlers ?? throw new ArgumentNullException(nameof(reactionHandlers))).ToArray();

            DiscordClient.ReactionAdded += DiscordClient_ReactionAdded;
        }
        private async Task DiscordClient_ReactionAdded(Cacheable<Discord.IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            
            if (arg3.Message.GetValueOrDefault() is not IUserMessage message)
            {
                try
                {
                    message = (await arg2.GetMessageAsync(arg3.MessageId)) as IUserMessage;
                    if (message is null)
                        return;
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Could not retrieve non-cached message that has been reacted to.");
                    return;
                }
            }

            if (arg3.User.GetValueOrDefault() is not IUser user)
            {
                try
                {
                    user = await DiscordClient.Rest.GetUserAsync(arg3.UserId);
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
                    var reactionHandled = await reactionHandler.Handle(message, arg3.Emote, user);
                    if (reactionHandled)
                        break;
                }
                catch (Exception e)
                {
                    Logger.LogError(
                        "Reaction handler {0} threw an exception when handling reaction {1} added to message {2}.",
                        reactionHandler,
                        arg3.Emote.Name,
                        message.Id
                    );
                }
            }            
        }
    }
}
