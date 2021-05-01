using Discord;
using Inkluzitron.Contracts;
using Inkluzitron.Data;
using Inkluzitron.Extensions;
using Inkluzitron.Models.Settings;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Inkluzitron.Modules.BdsmTestOrg
{
    public class QuizEmbedManager : IReactionHandler
    {
        protected BotDatabaseContext DbContext { get; }
        protected ReactionSettings ReactionSettings { get; }
        protected BdsmTestOrgSettings Settings { get; }

        public QuizEmbedManager(BotDatabaseContext dbContext, ReactionSettings reactionSettings, BdsmTestOrgSettings settings)
        {
            DbContext = dbContext;
            ReactionSettings = reactionSettings;
            Settings = settings;
        }

        public async Task<bool> Handle(IUserMessage message, IEmote reaction, IUser user, IUser self)
        {
            if (message.Author.Id != self.Id)
                return false;

            if (message.Embeds.Count != 1)
                return false;

            var embed = message.Embeds.Single();

            if (!embed.TryParseMetadata<QuizEmbedMetadata>(out var metadata))
                return false;

            if (!ReactionSettings.PaginationReactionsWithRemoval.Contains(reaction))
                return false;

            var currentPageResultWasRemoved = false;
            if (metadata.UserId == user.Id && reaction.Equals(ReactionSettings.Remove))
            {
                var result = await DbContext.BdsmTestOrgQuizResults.FindAsync(metadata.ResultId);
                if (result != null)
                {
                    DbContext.BdsmTestOrgQuizResults.Remove(result);
                    await DbContext.SaveChangesAsync();
                    currentPageResultWasRemoved = true;
                }
            }

            var quizResultsOfUser = DbContext
                .BdsmTestOrgQuizResults
                .Include(x => x.Items)
                .AsAsyncEnumerable()
                .Where(r => r.SubmittedById == metadata.UserId)
                .OrderByDescending(r => r.SubmittedAt);

            var count = await quizResultsOfUser.CountAsync();

            var formerPageNumber = metadata.PageNumber;
            int newPageNumber;
            if (reaction.Equals(ReactionSettings.MoveToFirst))
                newPageNumber = 1;
            else if (reaction.Equals(ReactionSettings.MoveToPrevious))
                newPageNumber = formerPageNumber - 1;
            else if (reaction.Equals(ReactionSettings.MoveToNext))
                newPageNumber = formerPageNumber + 1;
            else if (reaction.Equals(ReactionSettings.MoveToLast))
                newPageNumber = count;
            else
                newPageNumber = formerPageNumber;

            if (newPageNumber < 1)
                newPageNumber = 1;
            else if (newPageNumber > count)
                newPageNumber = count;

            if (newPageNumber != formerPageNumber || currentPageResultWasRemoved)
            {
                var newResultToDisplay = await quizResultsOfUser
                    .Skip(newPageNumber - 1)
                    .FirstOrDefaultAsync();

                var newEmbed = new EmbedBuilder().WithAuthor(user);

                if (newResultToDisplay is null)
                    newEmbed = newEmbed.WithBdsmTestOrgQuizInvitation(user, Settings);
                else
                    newEmbed = newEmbed.WithBdsmTestOrgQuizResult(newResultToDisplay, newPageNumber, count);

                await message.ModifyAsync(p => p.Embed = newEmbed.Build());
            }

            await message.RemoveReactionAsync(reaction, user);
            return true;
        }
    }
}
