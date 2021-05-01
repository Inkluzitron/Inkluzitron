using Discord;
using Discord.Commands;
using Inkluzitron.Data;
using Inkluzitron.Data.Entities;
using Inkluzitron.Models.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Inkluzitron.Modules.BdsmTestOrg
{
    public class QuizModule : ModuleBase
    {
        static private readonly Regex TestResultRegex = new(
            @"==\sResults\sfrom\sbdsmtest.org\s==\s+
    (?<results>.+)
    (?<link>https?://bdsmtest\.org/r/[\d\w]+)",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline
        );

        static private readonly Regex TestResultItemRegex = new(
            @"^(?<pctg>\d+)%\s+(?<trait>[^\n]+?)\s*$",
            RegexOptions.Multiline
        );

        private BotDatabaseContext DbContext { get; }
        private ReactionSettings ReactionSettings { get; }
        private BdsmTestOrgSettings Settings { get; }

        public QuizModule(BotDatabaseContext dbContext, ReactionSettings reactionSettings, BdsmTestOrgSettings bdsmTestOrgSettings)
        {
            DbContext = dbContext;
            ReactionSettings = reactionSettings;
            Settings = bdsmTestOrgSettings;
        }

        [Command("bdsmtest")]
        [Alias("bdsm")]
        [Summary("Zobrazí výsledky BdsmTest.org uživatele, nebo takový výsledek přidá do databáze.")]
        public async Task HandleBdsmTestOrgCommandAsync(params string[] strings)
        {
            if (strings.Length == 0)
                await ReplyWithBdsmTestEmbed();
            else
                await ProcessQuizResultSubmission();
        }

        private async Task ReplyWithBdsmTestEmbed()
        {
            var authorId = Context.Message.Author.Id;
            var quizResultsOfUser = DbContext.BdsmTestOrgQuizResults
                .Include(x => x.Items)
                .Where(x => x.SubmittedById == authorId);

            var pageCount = await quizResultsOfUser.CountAsync();
            var mostRecentResult = await quizResultsOfUser.OrderByDescending(r => r.SubmittedAt)
                .Take(1)
                .FirstOrDefaultAsync();

            var embedBuilder = new EmbedBuilder().WithAuthor(Context.Message.Author);

            if (mostRecentResult is null)
                embedBuilder = embedBuilder.WithBdsmTestOrgQuizInvitation(Context.Message.Author, Settings);
            else
                embedBuilder = embedBuilder.WithBdsmTestOrgQuizResult(mostRecentResult, 1, pageCount);

            var message = await ReplyAsync(embed: embedBuilder.Build());
            await message.AddReactionsAsync(ReactionSettings.PaginationReactionsWithRemoval);
        }

        private async Task ProcessQuizResultSubmission()
        {
            var reconstructedMessage = Context.Message.ToString();
            var testResultMatches = TestResultRegex.Matches(reconstructedMessage);
            if (testResultMatches.Count == 0)
            {
                await ReplyAsync(Settings.InvalidFormatMessage);
                return;
            }

            var testResultMatch = testResultMatches.Single();
            var testResultItems = testResultMatch.Groups["results"].Value;
            var testResultLink = testResultMatch.Groups["link"].Value;

            if (await DbContext.BdsmTestOrgQuizResults.AsAsyncEnumerable().AnyAsync(r => r.Link == testResultLink))
            {
                await ReplyAsync(Settings.LinkAlreadyPresentMessage);
                return;
            }

            var itemsMatch = TestResultItemRegex.Matches(testResultItems);

            var testResult = new BdsmTestOrgQuizResult
            {
                SubmittedAt = DateTime.UtcNow,
                SubmittedByName = Context.Message.Author.Username,
                SubmittedById = Context.Message.Author.Id,
                Link = testResultLink
            };

            foreach (Match match in itemsMatch)
            {
                var traitName = match.Groups["trait"].Value;
                if (!Settings.TraitList.Contains(traitName))
                {
                    await ReplyAsync(Settings.InvalidTraitMessage);
                    return;
                }

                if (!TryParseTraitPercentage(match.Groups["pctg"].Value, out var traitPercentage))
                {
                    await ReplyAsync(Settings.InvalidPercentageMessage);
                    return;
                }

                testResult.Items.Add(new QuizDoubleItem
                {
                    Key = traitName,
                    Value = traitPercentage
                });
            }

            if (testResult.Items.Count != Settings.TraitList.Count)
            {
                await ReplyAsync(Settings.InvalidTraitCountMessage);
                return;
            }

            await DbContext.BdsmTestOrgQuizResults.AddAsync(testResult);
            await DbContext.SaveChangesAsync();
            await Context.Message.AddReactionAsync(ReactionSettings.BdsmTestResultAdded);
        }

        static private bool TryParseTraitPercentage(string traitPercentage, out double percentage)
        {
            var inputIsValid = int.TryParse(traitPercentage, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integralPercentage)
                && integralPercentage >= 0
                && integralPercentage <= 100;

            if (!inputIsValid)
            {
                percentage = 0;
                return false;
            }

            percentage = integralPercentage / 100.0;
            return true;
        }
    }
}
