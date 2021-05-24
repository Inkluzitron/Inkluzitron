using Discord;
using Discord.Commands;
using Inkluzitron.Data;
using Inkluzitron.Data.Entities;
using Inkluzitron.Extensions;
using Inkluzitron.Models.Settings;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using Inkluzitron.Models.BdsmTestOrgApi;
using Inkluzitron.Enums;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Inkluzitron.Services;
using Inkluzitron.Utilities;

namespace Inkluzitron.Modules.BdsmTestOrg
{
    [Name("BDSMTest.org")]
    [Group("bdsm")]
    [Summary("Dotaz na kategorie může mít následující podoby:\n`dom sub` == `dom>50 sub>50` == `+dom +sub`\n`dom -switch` == `dom switch<50`")]
    public class BdsmModule : ModuleBase
    {
        static private readonly Regex TestResultLinkRegex = new(@"^https?://bdsmtest\.org/r/([\d\w]+)");

        private BotDatabaseContext DbContext { get; set; }
        private DatabaseFactory DatabaseFactory { get; }
        private ReactionSettings ReactionSettings { get; }
        private BdsmTestOrgSettings Settings { get; }
        private GraphPaintingService GraphPainter { get; }
        private IHttpClientFactory HttpClientFactory { get; }
        public UserBdsmTraitsService BdsmTraitsService { get; }

        public BdsmModule(DatabaseFactory databaseFactory,
            ReactionSettings reactionSettings, BdsmTestOrgSettings bdsmTestOrgSettings,
            GraphPaintingService graphPainter, IHttpClientFactory factory,
            UserBdsmTraitsService bdsmTraitsService)
        {
            DatabaseFactory = databaseFactory;
            ReactionSettings = reactionSettings;
            Settings = bdsmTestOrgSettings;
            GraphPainter = graphPainter;
            HttpClientFactory = factory;
            BdsmTraitsService = bdsmTraitsService;
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

        [Command("")]
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

        [Command("stats")]
        [Alias("gdo", "kdo", "graph")]
        [Summary("Sestaví a zobrazí žebříček výsledků a vykreslí jej do grafu. Volitelně je možné výsledky filtrovat.")]
        public async Task DrawStatsGraphAsync([Name("kritéria...")][Optional] params string[] categoriesQuery)
        {
            var resultsDict = await ProcessQueryAsync(categoriesQuery);

            if (resultsDict.All(o => o.Value.Count == 0))
            {
                await ReplyAsync(Settings.NoContentToStats);
                return;
            }

            using var imgFile = new TemporaryFile("png");
            using var img = await GraphPainter.DrawAsync(Context.Guild, resultsDict);
            img.Save(imgFile.Path, System.Drawing.Imaging.ImageFormat.Png);
            await ReplyFileAsync(imgFile.Path);
        }

        [Command("list")]
        [Summary("Sestaví a zobrazí seznam z výsledků. Volitelně je možné výsledky filtrovat.")]
        public async Task SearchAndTextAnswerAsync([Name("kritéria...")][Optional] params string[] query)
        {
            var resultsDict = await ProcessQueryAsync(query);
            var results = new StringBuilder();

            foreach (var trait in resultsDict.Keys.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                string resultsLine;

                var resultsGot = resultsDict.TryGetValue(trait, out var items) && items.Count > 0;
                if (!resultsGot) continue;

                resultsLine = string.Join(", ", items.Select(i => $"**`{i.Parent.SubmittedByName}`** ({i.Value:P0})"));

                results.AppendFormat("**{0}**: ", trait);
                results.AppendLine(resultsLine);
            }

            if (results.Length == 0)
                results.Append(Settings.NoMatchesMessage);

            var parts = results.SplitToParts(DiscordConfig.MaxMessageSize);
            await ReplyAsync(parts, allowedMentions: new AllowedMentions(AllowedMentionTypes.None));
        }

        static private readonly Regex ComparisonRegex = new(@"^([^<>]+)([<>])(\d+)$");

        private async Task<IDictionary<string, List<QuizDoubleItem>>> ProcessQueryAsync(params string[] query)
        {
            var resultsDict = new ConcurrentDictionary<string, List<QuizDoubleItem>>();
            var positiveFilters = new ConcurrentDictionary<string, double>();
            var negativeFilters = new ConcurrentDictionary<string, double>();

            var namedTraits = Enum.GetValues<BdsmTraits>()
                .Select(t => t.GetType()
                    .GetMember(t.ToString())[0]
                    .GetCustomAttribute<DisplayAttribute>()
                    .GetName());

            foreach (var rawQueryItem in query)
            {
                var isNegativeQuery = false;
                var threshold = Settings.StrongTraitThreshold;
                var queryItem = rawQueryItem;

                if (queryItem.StartsWith('+') || queryItem.StartsWith('-'))
                {
                    isNegativeQuery = queryItem.StartsWith('-');
                    queryItem = queryItem[1..];
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
                foreach (var traitName in namedTraits)
                {
                    if (!traitName.Contains(queryItem, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (isNegativeQuery)
                        matchFound |= negativeFilters.TryAdd(traitName, threshold);
                    else
                        matchFound |= positiveFilters.TryAdd(traitName, threshold);
                }

                if (!matchFound)
                {
                    await ReplyAsync($"{Settings.BadFilterQueryMessage}: {rawQueryItem}");
                    return resultsDict;
                }
            }

            if (positiveFilters.IsEmpty)
            {
                foreach (var traitName in namedTraits)
                    positiveFilters.TryAdd(traitName, Settings.StrongTraitThreshold);
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

            foreach (var testResult in resultsDict.Values.SelectMany(x => x).Select(x => x.Parent))
            {
                var guildMember = await Context.Guild.GetUserAsync(testResult.SubmittedById);
                if (guildMember == null)
                    continue;

                testResult.SubmittedByName = guildMember.Nickname
                    ?? guildMember.Username
                    ?? testResult.SubmittedByName;
            }

            return resultsDict;
        }

        [Command("add")]
        [Summary("Přidá do databáze výsledek testu.")]
        public async Task ProcessQuizResultSubmissionAsync([Name("odkaz")] string link)
        {
            var testResultMatch = TestResultLinkRegex.Match(link);
            if (!testResultMatch.Success)
            {
                await ReplyAsync(Settings.InvalidFormatMessage);
                return;
            }

            link = testResultMatch.Value;

            if (await DbContext.BdsmTestOrgQuizResults.AsQueryable().AnyAsync(r => r.Link == link))
            {
                await ReplyAsync(Settings.LinkAlreadyPresentMessage);
                return;
            }

            var testid = testResultMatch.Groups[1].Value;

            var requestData = new Dictionary<string, string>
            {
                { "uauth[uid]", Settings.ApiKey.Uid },
                { "uauth[salt]", Settings.ApiKey.Salt },
                { "uauth[authsig]", Settings.ApiKey.AuthSig },
                { "rauth[rid]", testid }
            };

            var response = await HttpClientFactory.CreateClient().PostAsync(
                "https://bdsmtest.org/ajax/getresult",
                new FormUrlEncodedContent(requestData));

            var responseData = await response.Content.ReadAsStringAsync();
            var testResult = JsonConvert.DeserializeObject<Result>(responseData);

            var testResultDb = new BdsmTestOrgQuizResult
            {
                SubmittedAt = testResult.Date,
                SubmittedByName = Context.Message.Author.Username,
                SubmittedById = Context.Message.Author.Id,
                Link = link
            };

            foreach (var trait in testResult.Traits)
            {
                if (!Enum.IsDefined(typeof(BdsmTraits), trait.Id)) continue;

                var percentage = trait.Score / 100.0;
                if (percentage > 1) percentage = 1;
                else if (percentage < 0) percentage = 0;

                testResultDb.Items.Add(new QuizDoubleItem
                {
                    Key = trait.Name,
                    Value = percentage
                });
            }

            await DbContext.BdsmTestOrgQuizResults.AddAsync(testResultDb);
            await DbContext.SaveChangesAsync();
            await Context.Message.AddReactionAsync(ReactionSettings.BdsmTestResultAdded);

            await ShowUserResultsAsync();
        }

        [Command("last roll")]
        [Summary("Vypíše poslední započítaný vliv výsledků BDSM testu, např. vůči použitému příkazu whip.")]
        public async Task ShowLastOperationCheckAsync()
        {
            if (!BdsmTraitsService.TryGetLastOperationCheck(Context.User, out var lastCheck))
            {
                await Context.Message.AddReactionAsync(ReactionSettings.Shrunk);
                return;
            }

            await ReplyAsync(lastCheck.ToString());
        }
    }
}
