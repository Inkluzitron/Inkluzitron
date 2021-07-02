using Discord;
using Inkluzitron.Contracts;
using Inkluzitron.Enums;
using Inkluzitron.Models;
using System.Threading.Tasks;

namespace Inkluzitron.Modules.Vote
{
    public class VoteReactionHandler : IReactionHandler
    {
        private VoteService VoteService { get; }

        public VoteReactionHandler(VoteService voteService)
        {
            VoteService = voteService;
        }

        public async Task<bool> HandleReactionChangedAsync(IUserMessage message, IEmote reaction, IUser user, ReactionEvent eventType)
        {
            if (user.IsBot)
                return false;

            if (await VoteService.TryParseVoteCommand(message) is not VoteDefinition voteDefinition)
                return false;

            if (voteDefinition.IsPastDeadline())
            {
                // vote already finished
                await message.RemoveReactionAsync(reaction, user);
                return true;
            }

            if (!voteDefinition.Options.ContainsKey(reaction))
            {
                // not a vote option
                await message.RemoveReactionAsync(reaction, user);
                return true;
            }

            var newText = VoteService.ComposeSummary(message, voteDefinition);
            await VoteService.UpdateVoteReplyAsync(message, newText);
            return true;
        }
    }
}
