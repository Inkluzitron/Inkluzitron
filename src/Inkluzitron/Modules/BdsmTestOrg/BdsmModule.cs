using Discord;
using Discord.Commands;
using Inkluzitron.Data;
using Inkluzitron.Data.Entities;
using Inkluzitron.Models.Settings;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Inkluzitron.Modules.BdsmTestOrg
{
    [Name("BDSMTest.org")]
    [Group("bdsm")]
    public class BdsmModule : ModuleBase
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

        public BdsmModule(BotDatabaseContext dbContext, ReactionSettings reactionSettings, BdsmTestOrgSettings bdsmTestOrgSettings)
        {
            DbContext = dbContext;
            ReactionSettings = reactionSettings;
            Settings = bdsmTestOrgSettings;
        }

        [Command]
        [Name("")]
        [Summary("Zobrazí výsledky uživatele.")]
        public async Task ShowUserResultsAsync()
        {
            var authorId = Context.Message.Author.Id;
            var quizResultsOfUser = DbContext.BdsmTestOrgQuizResults
                .Include(x => x.Items)
                .Where(x => x.SubmittedById == authorId);

            var pageCount = await quizResultsOfUser.CountAsync();
            var mostRecentResult = await quizResultsOfUser.OrderByDescending(r => r.SubmittedAt)
                .FirstOrDefaultAsync();

            var embedBuilder = new EmbedBuilder().WithAuthor(Context.Message.Author);

            if (mostRecentResult is null)
                embedBuilder = embedBuilder.WithBdsmTestOrgQuizInvitation(Settings, Context.Message.Author);
            else
                embedBuilder = embedBuilder.WithBdsmTestOrgQuizResult(Settings, mostRecentResult, 1, pageCount);

            var message = await ReplyAsync(embed: embedBuilder.Build());
            await message.AddReactionsAsync(ReactionSettings.PaginationReactionsWithRemoval);
        }

        [Command("gdo")]
        [Summary("Sestaví a zobrazí žebříček z výsledků zadaných ostatními uživateli serveru.")]
        public Task SearchAsync()
            => ProcessQueryAsync(false);

        [Command("gdo")]
        [Summary("Sestaví a zobrazí žebříček z uvedených kategorií výsledků zadaných ostatními uživateli serveru.")]
        public Task FilteredSearchAsync([Name("názvyKategorií")] params string[] categoriesToShow)
            => ProcessQueryAsync(false, categoriesToShow);

        [Command("GDO")]
        [Summary("Sestaví a zobrazí žebříček z výsledků zadaných ostatními uživateli serveru. Zobrazí i kategorie, ve kterých nejsou relevantní výsledky.")]
        public Task VerboseSearchAsync()
           => ProcessQueryAsync(true);

        [Command("GDO")]
        [Summary("Sestaví a zobrazí žebříček z uvedených kategorií výsledků zadaných ostatními uživateli serveru. Zobrazí i kategorie, ve kterých nejsou relevantní výsledky.")]
        public Task FilteredVerboseSearchAsync([Name("názvyKategorií")] params string[] categoriesToShow)
           => ProcessQueryAsync(true, categoriesToShow);

        private async Task ProcessQueryAsync(bool showAllMode, params string[] query)
        {
            var traitsToSearchFor = new HashSet<string>();

            foreach (var queryItem in query)
            {
                var matchFound = false;

                foreach (var trait in Settings.TraitList)
                {
                    if (trait.Contains(queryItem, StringComparison.OrdinalIgnoreCase))
                        matchFound |= traitsToSearchFor.Add(trait);
                }

                if (!matchFound)
                {
                    await ReplyAsync($"{Settings.BadFilterQueryMessage}: {queryItem}");
                    return;
                }
            }

            if (traitsToSearchFor.Count == 0)
                traitsToSearchFor.UnionWith(Settings.TraitList);

            var relevantItems = DbContext.BdsmTestOrgQuizResults.AsQueryable()
                .GroupBy(r => r.SubmittedById, (_, results) => results.Max(r => r.ResultId))
                .Join(
                    DbContext.DoubleQuizItems.Include(i => i.Parent).Where(i => i.Parent is BdsmTestOrgQuizResult),
                    x => x,
                    y => y.Parent.ResultId,
                    (_, r) => r
                );

            var resultsDict = new ConcurrentDictionary<string, List<QuizDoubleItem>>();
            foreach (var trait in traitsToSearchFor)
            {
                resultsDict[trait] = relevantItems
                    .Where(i => i.Key == trait)
                    .Where(i => i.Value > Settings.TraitReportingThreshold)
                    .OrderByDescending(row => row.Value)
                    .ThenByDescending(row => row.Parent.SubmittedAt)
                    .Take(Settings.MaximumMatchCount)
                    .ToList();
            }

            var results = new StringBuilder();
            foreach (var trait in traitsToSearchFor.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                string resultsLine;

                var resultsGot = resultsDict.TryGetValue(trait, out var items) && items.Count > 0;
                if (resultsGot)
                    resultsLine = string.Join(", ", items.Select(i => $"<@{i.Parent.SubmittedById}> ({i.Value:P0})"));
                else if (showAllMode)
                    resultsLine = Settings.NoMatchesMessage;
                else
                    continue;

                results.AppendFormat("**{0}**: ", trait);
                results.AppendLine(resultsLine);
            }

            if (results.Length == 0)
                results.Append(Settings.NoMatchesMessage);

            await ReplyAsync(results.ToString(), allowedMentions: new AllowedMentions(AllowedMentionTypes.None));
        }

        [Command("add")]
        [Summary("Přidá do databáze výsledek testu.")]
        public async Task ProcessQuizResultSubmissionAsync([Name("<textová forma výsledků>")] params string[] textResults)
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

            if (await DbContext.BdsmTestOrgQuizResults.AsQueryable().AnyAsync(r => r.Link == testResultLink))
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
