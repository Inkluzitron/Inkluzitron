using Discord;
using Inkluzitron.Contracts;
using Inkluzitron.Data;
using Inkluzitron.Models.Settings;
using Inkluzitron.Modules.BdsmTestOrg;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Inkluzitron.Modules.BdsmTestOrg
{
    public class QuizEmbedManager : IReactionHandler
    {
        protected BotDatabaseContext DbContext { get; }
        protected ReactionSettings Settings { get; }

        public QuizEmbedManager(BotDatabaseContext dbContext, ReactionSettings settings)
        {
            DbContext = dbContext;
            Settings = settings;
        }

        public async Task<bool> Handle(IUserMessage message, IEmote reaction, IUser user)
        {
            if (message.Embeds.Count != 1)
                return false;

            var embed = message.Embeds.Single();
            if (embed.Author is not EmbedAuthor embedAuthor)
                return false;

            if (embed.Footer is not EmbedFooter footer)
                return false;

            if (!Settings.PaginationReactions.Contains(reaction))
                return false;

            var footerText = embed.Footer.Value.Text;
            if (!QuizEmbedBuilder.TryParseFooterText(footerText, out var authorId, out var formerPageNumber))
                return false;

            var quizResultsOfUser = DbContext
                .BdsmTestOrgQuizResults
                .Include(x => x.Items)
                .AsAsyncEnumerable()
                .Where(r => r.SubmittedById == authorId);

            var count = await quizResultsOfUser.CountAsync();

            int newPageNumber;
            if (reaction.Equals(Settings.MoveToFirst))
                newPageNumber = 1;
            else if (reaction.Equals(Settings.MoveToPrevious))
                newPageNumber = formerPageNumber - 1;
            else if (reaction.Equals(Settings.MoveToNext))
                newPageNumber = formerPageNumber + 1;
            else if (reaction.Equals(Settings.MoveToLast))
                newPageNumber = count;
            else
                throw new NotSupportedException($"Don't know how to handle reaction '{reaction}'");

            if (newPageNumber < 1)
                newPageNumber = 1;
            else if (newPageNumber > count)
                newPageNumber = count;

            if (newPageNumber != formerPageNumber)
            {
                var newResultToDisplay = await quizResultsOfUser.OrderByDescending(r => r.SubmittedAt)
                    .Skip(newPageNumber - 1)
                    .FirstOrDefaultAsync();

                if (newResultToDisplay is null)
                    return false;

                var newEmbed = new QuizEmbedBuilder()
                    .WithQuizResult(newResultToDisplay, newPageNumber, count)
                    .WithAuthor(embed.Author.Value.Name, embed.Author.Value.IconUrl, embed.Author.Value.Url)
                    .Build();

                await message.ModifyAsync(p => p.Embed = newEmbed);
            }

            await message.RemoveReactionAsync(reaction, user);
            return true;
        }
    }
}
