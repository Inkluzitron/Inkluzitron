using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Inkluzitron.Contracts;
using Inkluzitron.Data;
using Inkluzitron.Extensions;
using Inkluzitron.Models.Settings;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace Inkluzitron.Modules.BdsmTestOrg
{
    public class QuizEmbedManager : IReactionHandler
    {
        protected DatabaseFactory DatabaseFactory { get; }
        protected ReactionSettings ReactionSettings { get; }
        protected BdsmTestOrgSettings Settings { get; }
        protected DiscordSocketClient Client { get; }

        public QuizEmbedManager(DatabaseFactory databaseFactory, ReactionSettings reactionSettings, BdsmTestOrgSettings settings,
            DiscordSocketClient client)
        {
            DatabaseFactory = databaseFactory;
            ReactionSettings = reactionSettings;
            Settings = settings;
            Client = client;
        }

        public async Task<bool> HandleReactionAddedAsync(IUserMessage message, IEmote reaction, IUser user)
        {
            if (message.ReferencedMessage == null || message.Embeds.Count != 1)
                return false;

            var embed = message.Embeds.Single();

            if (!embed.TryParseMetadata<QuizEmbedMetadata>(out var metadata))
                return false;

            if (!ReactionSettings.PaginationReactionsWithRemoval.Any(o => o.IsEqual(reaction)))
                return false;

            using var dbContext = DatabaseFactory.Create();
            var currentPageResultWasRemoved = false;
            if (metadata.UserId == user.Id && reaction.IsEqual(ReactionSettings.Remove))
            {
                var result = await dbContext.BdsmTestOrgQuizResults.FindAsync(metadata.ResultId);
                if (result != null)
                {
                    dbContext.BdsmTestOrgQuizResults.Remove(result);
                    await dbContext.SaveChangesAsync();
                    currentPageResultWasRemoved = true;
                }
            }

            if (!currentPageResultWasRemoved)
                currentPageResultWasRemoved = (await dbContext.BdsmTestOrgQuizResults.FindAsync(metadata.ResultId)) is null;

            var quizResultsOfUser = dbContext.BdsmTestOrgQuizResults
                .Include(x => x.Items)
                .AsQueryable()
                .Where(r => r.SubmittedById == metadata.UserId)
                .OrderByDescending(r => r.SubmittedAt);

            var count = await quizResultsOfUser.CountAsync();

            var formerPageNumber = metadata.PageNumber;
            int newPageNumber;
            if (reaction.IsEqual(ReactionSettings.MoveToFirst))
                newPageNumber = 1;
            else if (reaction.IsEqual(ReactionSettings.MoveToPrevious))
                newPageNumber = formerPageNumber - 1;
            else if (reaction.IsEqual(ReactionSettings.MoveToNext))
                newPageNumber = formerPageNumber + 1;
            else if (reaction.IsEqual(ReactionSettings.MoveToLast))
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

                var author = await Client.Rest.GetUserAsync(metadata.UserId);
                var newEmbed = new EmbedBuilder().WithAuthor(author);

                if (newResultToDisplay is null)
                    newEmbed = newEmbed.WithBdsmTestOrgQuizInvitation(Settings, author);
                else
                    newEmbed = newEmbed.WithBdsmTestOrgQuizResult(Settings, newResultToDisplay, newPageNumber, count);

                await message.ModifyAsync(p => p.Embed = newEmbed.Build());
            }

            var context = new CommandContext(Client, message.ReferencedMessage);
            if (!context.IsPrivate)
                await message.RemoveReactionAsync(reaction, user);

            return true;
        }
    }
}
