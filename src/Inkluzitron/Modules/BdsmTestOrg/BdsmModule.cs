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
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using Inkluzitron.Models.BdsmTestOrgApi;
using Inkluzitron.Enums;
using Inkluzitron.Services;
using Inkluzitron.Utilities;
using Inkluzitron.Models;

namespace Inkluzitron.Modules.BdsmTestOrg
{
    [Name("BDSMTest.org")]
    [Group("bdsm")]
    [Summary("Dotaz na kategorie může mít následující podoby:\n`dom sub` == `dom>50 sub>50` == `+dom +sub`\n`dom -switch` == `dom switch<50`")]
    public class BdsmModule : ModuleBase
    {
        static public readonly Regex TestResultLinkRegex = new(@"^https?://bdsmtest\.org/r/([\d\w]+)");
        static private readonly Regex ComparisonRegex = new(@"^([^<>]+)([<>])(\d+)$");

        private BotDatabaseContext DbContext { get; set; }
        private DatabaseFactory DatabaseFactory { get; }
        private ReactionSettings ReactionSettings { get; }
        private BdsmTestOrgSettings Settings { get; }
        private IHttpClientFactory HttpClientFactory { get; }
        private UserBdsmTraitsService BdsmTraitsService { get; }
        private GraphPaintingService GraphPaintingService { get; }
        private BdsmGraphPaintingStrategy GraphPaintingStrategy { get; }
        private UsersService UsersService { get; }

        public BdsmModule(DatabaseFactory databaseFactory,
            ReactionSettings reactionSettings, BdsmTestOrgSettings bdsmTestOrgSettings,
            GraphPaintingService graphPainter, IHttpClientFactory factory,
            UserBdsmTraitsService bdsmTraitsService, UsersService usersService,
            BdsmGraphPaintingStrategy graphPaintingStrategy)
        {
            DatabaseFactory = databaseFactory;
            ReactionSettings = reactionSettings;
            Settings = bdsmTestOrgSettings;
            HttpClientFactory = factory;
            BdsmTraitsService = bdsmTraitsService;
            GraphPaintingService = graphPainter;
            GraphPaintingStrategy = graphPaintingStrategy;
            UsersService = usersService;
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
        [Summary("Zobrazí výsledky odesílatele nebo uživatele.")]
        public async Task ShowUserResultsAsync([Name("koho")] IUser target = null)
        {
            if (target is null)
                target = Context.Message.Author;

            var quizResultsOfUser = DbContext.BdsmTestOrgResults.Include(x => x.Items)
                .Where(x => x.UserId == target.Id);

            var pageCount = await quizResultsOfUser.CountAsync();
            var mostRecentResult = await quizResultsOfUser.OrderByDescending(r => r.SubmittedAt)
                .FirstOrDefaultAsync();

            var embedBuilder = new EmbedBuilder().WithAuthor(target);

            if (mostRecentResult is null)
                embedBuilder = embedBuilder.WithBdsmTestOrgQuizInvitation(Settings, target);
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
            await using var _ = await DisposableReaction.CreateAsync(Context.Message, ReactionSettings.Loading, Context.Client.CurrentUser);
            var resultsDict = await ProcessQueryAsync(categoriesQuery);

            if (resultsDict.Count == 0) return;

            if (resultsDict.All(o => o.Value.Count == 0))
            {
                await ReplyAsync(Settings.NoContentToStats);
                return;
            }

            using var imgFile = new TemporaryFile("png");
            using var img = await GraphPaintingService.DrawAsync(Context.Guild, GraphPaintingStrategy, resultsDict);
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
                if (!resultsGot)
                    continue;

                resultsLine = string.Join(", ", items.Select(i => $"**`{i.UserDisplayName}`** ({i.Value:P0})"));
                results.AppendFormat("**{0}**: ", trait);
                results.AppendLine(resultsLine);
            }

            if (results.Length == 0)
                results.Append(Settings.NoMatchesMessage);

            var parts = results.SplitToParts(DiscordConfig.MaxMessageSize);
            await ReplyAsync(parts, allowedMentions: new AllowedMentions(AllowedMentionTypes.None));
        }

        private async Task<IDictionary<string, IReadOnlyList<GraphItem>>> ProcessQueryAsync(params string[] query)
        {
            var resultsDict = new ConcurrentDictionary<string, IReadOnlyList<GraphItem>>();
            var positiveFilters = new ConcurrentDictionary<BdsmTrait, double>();
            var negativeFilters = new ConcurrentDictionary<BdsmTrait, double>();
            var explicitlyRequestedTraits = new HashSet<BdsmTrait>();

            var availableTraits = Enum.GetValues<BdsmTrait>();

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

                var matchesFound = 0;
                BdsmTrait? lastMatch = null;

                foreach (var trait in availableTraits)
                {
                    if (!trait.GetDisplayName().Contains(queryItem, StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (isNegativeQuery)
                    {
                        if (explicitlyRequestedTraits.Contains(trait))
                            continue;

                        if (negativeFilters.TryAdd(trait, threshold))
                        {
                            matchesFound++;
                            lastMatch = trait;
                        }
                    }
                    else
                    {
                        if (positiveFilters.TryAdd(trait, threshold))
                        {
                            matchesFound++;
                            lastMatch = trait;
                        }
                    }
                }

                if (matchesFound == 0)
                {
                    await ReplyAsync($"{Settings.BadFilterQueryMessage}: {rawQueryItem}");
                    return resultsDict;
                }
                else if (matchesFound == 1)
                {
                    explicitlyRequestedTraits.Add(lastMatch.Value);
                }
            }

            if (positiveFilters.IsEmpty)
            {
                foreach (var traitName in availableTraits)
                    positiveFilters.TryAdd(traitName, Settings.StrongTraitThreshold);
            }

            var relevantResults = DbContext.BdsmTestOrgResults.Include(i => i.Items).AsQueryable();

            foreach (var (trait, threshold) in negativeFilters)
                relevantResults = relevantResults.Where(r => r.Items.All(i => i.Trait != trait || i.Score < threshold));

            var relevantItems = relevantResults.Join(
                    DbContext.BdsmTestOrgItems.Include(i => i.Parent).ThenInclude(r => r.User),
                    x => x.Id,
                    y => y.Parent.Id,
                    (_, r) => r
                );

            foreach (var (trait, threshold) in positiveFilters)
            {
                resultsDict[trait.GetDisplayName()] = await relevantItems
                    .Where(i => i.Trait == trait)
                    .Where(i => i.Score > threshold)
                    .OrderByDescending(row => row.Score)
                    .ThenByDescending(row => row.Parent.SubmittedAt)
                    .Take(Settings.MaximumMatchCount)
                    .Select(i => new GraphItem {
                        UserId = i.Parent.UserId,
                        UserDisplayName = i.Parent.User.Name,
                        Value = i.Score
                    })
                    .ToListAsync();
            }

            foreach (var testResult in resultsDict.Values.SelectMany(x => x))
            {
                var guildMember = await Context.Guild.GetUserAsync(testResult.UserId);
                if (guildMember == null)
                    continue;

                testResult.UserDisplayName= guildMember.Nickname
                    ?? guildMember.Username
                    ?? testResult.UserDisplayName;
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

            if (await DbContext.BdsmTestOrgResults.AsQueryable().AnyAsync(r => r.Link == link))
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


            var user = await UsersService.GetOrCreateUserDbEntityAsync(
                Context.Message.Author);

            if (testResult.Gender != Gender.Unspecified)
                user.Gender = testResult.Gender;

            var testResultDb = new BdsmTestOrgResult
            {
                SubmittedAt = testResult.Date,
                UserId = user.Id,
                Link = link
            };

            foreach (var trait in testResult.Traits)
            {
                if (!Enum.IsDefined(typeof(BdsmTrait), trait.Id)) continue;

                var percentage = trait.Score / 100.0;
                if (percentage > 1) percentage = 1;
                else if (percentage < 0) percentage = 0;

                testResultDb.Items.Add(new BdsmTestOrgItem
                {
                    Trait = (BdsmTrait)trait.Id,
                    Score = percentage
                });
            }

            await DbContext.BdsmTestOrgResults.AddAsync(testResultDb);
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
