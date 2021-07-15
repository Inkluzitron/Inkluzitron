using Discord;
using Inkluzitron.Contracts;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Inkluzitron.Modules.Vote
{
    public class VoteMessageEventHandler : IMessageEventHandler
    {
        private VoteService VoteService { get; }
        public ILogger<VoteMessageEventHandler> Logger { get; }

        public VoteMessageEventHandler(VoteService voteService, ILogger<VoteMessageEventHandler> logger)
        {
            VoteService = voteService;
            Logger = logger;
        }

        public async Task<bool> HandleMessageUpdatedAsync(IMessageChannel channel, IMessage updatedMessage, Lazy<Task<IMessage>> freshMessageFactory)
        {
            try
            {
                var newMessage = await freshMessageFactory.Value;
                var (success, _, commandArgs) = await VoteService.TryMatchVoteCommand(newMessage);

                if (success)
                {
                    // newMessage has Reactions.Count == 0, always
                    var freshMessage = await channel.GetMessageAsync(newMessage.Id);
                    if (freshMessage is IUserMessage freshUserMessage)
                        await VoteService.ProcessVoteCommandAsync(freshUserMessage, commandArgs);

                    return true;
                }
                else
                {
                    await VoteService.DeleteAssociatedVoteReplyIfExistsAsync(channel, newMessage.Id);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Could not process updated message {0} in channel {1}", updatedMessage.Id, channel);
            }

            return false;
        }

        public async Task<bool> HandleMessageDeletedAsync(IMessageChannel channel, ulong messageId)
        {
            try
            {
                var wasVoteCommand = await VoteService.DeleteAssociatedVoteReplyIfExistsAsync(channel, messageId);
                if (!wasVoteCommand)
                    await VoteService.DeleteVoteReplyRecordIfExistsAsync(channel, messageId);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Could not process deleted message {0} in channel {1}", messageId, channel);
            }

            return false;
        }

        public async Task<bool> HandleMessagesBulkDeletedAsync(IMessageChannel channel, IReadOnlyCollection<ulong> messageIds)
        {
            foreach (var messageId in messageIds)
                await HandleMessageDeletedAsync(channel, messageId);

            return false;
        }
    }
}
