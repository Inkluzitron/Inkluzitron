using Discord;
using Discord.Commands;
using Inkluzitron.Data;
using Inkluzitron.Data.Entities;
using Inkluzitron.Extensions;
using Inkluzitron.Models.Settings;
using Inkluzitron.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Inkluzitron.Modules.BdsmTestOrg
{
    [Name("BDSMTest.org")]
    [Group("bdsm")]
    [Summary("Dotaz na kategorie může mít následující podoby:\n`dom sub` == `dom>50 sub>50` == `+dom +sub`\n`dom -switch` == `dom switch<50`")]
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

        private BotDatabaseContext DbContext { get; set; }
        private DatabaseFactory DatabaseFactory { get; }
        private ReactionSettings ReactionSettings { get; }
        private BdsmTestOrgSettings Settings { get; }
        private GraphPaintingService GraphPainter { get; }

        public BdsmModule(DatabaseFactory databaseFactory, ReactionSettings reactionSettings, BdsmTestOrgSettings bdsmTestOrgSettings, GraphPaintingService graphPainter)
        {
            DatabaseFactory = databaseFactory;
            ReactionSettings = reactionSettings;
            Settings = bdsmTestOrgSettings;
            GraphPainter = graphPainter;
        }

        protected override void BeforeExecute(CommandInfo command)
        {
            DbContext = DatabaseFactory.Create();
            base.BeforeExecute(command);
        }

        protected override void AfterExecute(CommandInfo command)
        {
            DbContext?.Dispose();
            base.AfterExecute(command);
        }

        [Command]
        [Name("")]
        [Summary("Zobrazí výsledky uživatele.")]
        public async Task ShowUserResultsAsync()
        {
            var authorId = Context.Message.Author.Id;
            var quizResultsOfUser = DbContext.BdsmTestOrgQuizResults.Include(x => x.Items)
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
            => SearchAndTextAnswerAsync(false);

        [Command("gdo")]
        [Summary("Sestaví a zobrazí žebříček z uvedených kategorií výsledků zadaných ostatními uživateli serveru.")]
        public Task FilteredSearchAsync([Name("dotazNaKategorie")] params string[] categoriesToShow)
            => SearchAndTextAnswerAsync(false, categoriesToShow);

        [Command("GDO")]
        [Summary("Sestaví a zobrazí žebříček z výsledků zadaných ostatními uživateli serveru. Zobrazí i kategorie, ve kterých nejsou relevantní výsledky.")]
        public Task VerboseSearchAsync()
           => SearchAndTextAnswerAsync(true);

        [Command("GDO")]
        [Summary("Sestaví a zobrazí žebříček z uvedených kategorií výsledků zadaných ostatními uživateli serveru. Zobrazí i kategorie, ve kterých nejsou relevantní výsledky.")]
        public Task FilteredVerboseSearchAsync([Name("dotazNaKategorie")] params string[] categoriesToShow)
           => SearchAndTextAnswerAsync(true, categoriesToShow);

        [Command("stats")]
        [Summary("Sestaví a zobrazí žebříček ze všech kategorií a vykreslí jej do grafu.")]
        public Task DrawStatsGraphAsync()
            => DrawStatsGraphAsync(Array.Empty<string>());

        [Command("stats")]
        [Summary("Sestaví a zobrazí žebříček z výsledků odpovídajících dotazu a vykreslí jej do grafu.")]
        public async Task DrawStatsGraphAsync([Name("dotazNaKategorie")] params string[] categoriesQuery)
        {
            var resultsDict = await ProcessQueryAsync(categoriesQuery);
            var imgFile = ImagesModule.CreateCachePath(Path.GetRandomFileName() + ".png");

            try
            {
                using var img = await GraphPainter.DrawAsync(resultsDict, Convert.ToSingle(Settings.TraitReportingThreshold));
                img.Save(imgFile, System.Drawing.Imaging.ImageFormat.Png);
                await ReplyFileAsync(imgFile);
            }
            finally
            {
                try
                {
                    File.Delete(imgFile);
                }
                catch {
                    // *** it's not much but it was honest work ***
                }
            }
        }

        private async Task SearchAndTextAnswerAsync(bool showAllMode, params string[] query)
        {
            var resultsDict = await ProcessQueryAsync(query);
            var results = new StringBuilder();

            foreach (var trait in resultsDict.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
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

            var parts = results.SplitToParts(DiscordConfig.MaxMessageSize);
            await ReplyAsync(parts, allowedMentions: new AllowedMentions(AllowedMentionTypes.None));
        }

        private static readonly Regex ComparisonRegex = new Regex(@"^([^<>]+)([<>])(\d+)$");

        private async Task<IDictionary<string, List<QuizDoubleItem>>> ProcessQueryAsync(params string[] query)
        {
            var resultsDict = new ConcurrentDictionary<string, List<QuizDoubleItem>>();
            var positiveFilters = new ConcurrentDictionary<string, double>();
            var negativeFilters = new ConcurrentDictionary<string, double>();

            foreach (var rawQueryItem in query)
            {
                var isNegativeQuery = false;
                var threshold = Settings.TraitReportingThreshold;
                var queryItem = rawQueryItem;

                if (queryItem.StartsWith('+') || queryItem.StartsWith('-'))
                {
                    isNegativeQuery = queryItem.StartsWith('-');
                    queryItem = queryItem.Substring(1);
                }
                else if (ComparisonRegex.Match(queryItem) is Match m && m.Success)
                {
                    queryItem = m.Groups[1].Value;
                    isNegativeQuery = m.Groups[2].Value == "<";
                    threshold = int.Parse(m.Groups[3].Value) / 100.0;
                    if (threshold < 0)
                        threshold = 0;
                    else if (threshold > 1)
                        threshold = 1;
                }

                var matchFound = false;
                foreach (var trait in Settings.TraitList)
                {
                    if (!trait.Contains(queryItem, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (isNegativeQuery)
                        matchFound |= negativeFilters.TryAdd(trait, threshold);
                    else
                        matchFound |= positiveFilters.TryAdd(trait, threshold);
                }

                if (!matchFound)
                {
                    await ReplyAsync($"{Settings.BadFilterQueryMessage}: {rawQueryItem}");
                    return resultsDict;
                }
            }

            if (positiveFilters.Count == 0)
            {
                foreach (var trait in Settings.TraitList)
                    positiveFilters.TryAdd(trait, Settings.TraitReportingThreshold);
            }

            var relevantResults = DbContext.BdsmTestOrgQuizResults.AsQueryable()
                .GroupBy(r => r.SubmittedById, (_, results) => results.Max(r => r.ResultId))
                .Join(
                    DbContext.BdsmTestOrgQuizResults.Include(i => i.Items).AsQueryable(),
                    id => id,
                    result => result.ResultId,
                    (_, result) => result
                );

            foreach (var (trait, threshold) in negativeFilters)
                relevantResults = relevantResults.Where(r => r.Items.OfType<QuizDoubleItem>().All(i => i.Key != trait || i.Value < threshold));

            var relevantItems = relevantResults.Join(
                    DbContext.DoubleQuizItems.Include(i => i.Parent).Where(i => i.Parent is BdsmTestOrgQuizResult),
                    x => x.ResultId,
                    y => y.Parent.ResultId,
                    (_, r) => r
                );

            foreach (var (trait, threshold) in positiveFilters)
            {
                resultsDict[trait] = await relevantItems
                    .Where(i => i.Key == trait)
                    .Where(i => i.Value > threshold)
                    .OrderByDescending(row => row.Value)
                    .ThenByDescending(row => row.Parent.SubmittedAt)
                    .Take(Settings.MaximumMatchCount)
                    .ToListAsync();
            }

            return resultsDict;
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
