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
    [Summary("Dotaz na kategorie se může skládat z následujících kritérií:\n" +
        "• `dom sub` == `+dom +sub` -> + zobrazí kategorie\n" +
        "• `+brat -tamer` -> - potlačí kategorie\n" +
        "• `brat>50` -> zobrazí jen uživatele s vyšší nebo rovnou hodnotou v kategorii\n" +
        "• `brat<50` -> zobrazí jen uživatele s nižší hodnotou v kategorii")]
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
            UserBdsmTraitsService bdsmTraitsService,
            BdsmGraphPaintingStrategy graphPaintingStrategy, UsersService usersService)
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

        [Command("graph")]
        [Alias("gdo", "kdo", "stats")]
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
            img.Write(imgFile.Path, ImageMagick.MagickFormat.Png);
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

            var traitsToShow = new HashSet<BdsmTrait>();
            var traitsToHide = new HashSet<BdsmTrait>();

            var greaterThanFilters = new ConcurrentDictionary<BdsmTrait, double>();
            var lowerThanFilters = new ConcurrentDictionary<BdsmTrait, double>();

            var availableTraits = Enum.GetValues<BdsmTrait>();

            foreach (var rawQueryItem in query)
            {
                Action<BdsmTrait> traitAcceptor;
                var queryItem = rawQueryItem;

                if (ComparisonRegex.Match(queryItem) is Match m && m.Success)
                {
                    queryItem = m.Groups[1].Value;
                    var threshold = int.Parse(m.Groups[3].Value) / 100.0;
                    if (threshold < 0)
                        threshold = 0;
                    else if (threshold > 1)
                        threshold = 1;

                    if (m.Groups[2].Value == "<")
                        traitAcceptor = t => lowerThanFilters.TryAdd(t, threshold);
                    else
                        traitAcceptor = t => greaterThanFilters.TryAdd(t, threshold);
                }
                else if (queryItem.StartsWith('-'))
                {
                    queryItem = queryItem[1..];
                    traitAcceptor = t => traitsToHide.Add(t);
                }
                else if (queryItem.StartsWith('+'))
                {
                    queryItem = queryItem[1..];
                    traitAcceptor = t => traitsToShow.Add(t);
                }
                else
                {
                    traitAcceptor = t => traitsToShow.Add(t);
                }

                var somethingFound = false;
                foreach (var trait in availableTraits)
                {
                    if (!trait.GetDisplayName().Contains(queryItem, StringComparison.OrdinalIgnoreCase))
                        continue;

                    somethingFound = true;
                    traitAcceptor(trait);
                }

                if (!somethingFound)
                {
                    await ReplyAsync($"{Settings.BadFilterQueryMessage} {rawQueryItem}");
                    return resultsDict;
                }
            }

            if (traitsToShow.Count == 0)
            {
                foreach (var traitName in availableTraits)
                    traitsToShow.Add(traitName);
            }

            traitsToShow.ExceptWith(traitsToHide);

            var dataSource = DbContext.BdsmTestOrgResults.Include(i => i.Items).AsQueryable();

            foreach (var (trait, threshold) in lowerThanFilters)
                dataSource = dataSource.Where(r => r.Items.All(i => i.Trait != trait || i.Score < threshold));

            foreach (var (trait, threshold) in greaterThanFilters)
                dataSource = dataSource.Where(r => r.Items.All(i => i.Trait != trait || i.Score >= threshold));

            var relevantItems = dataSource.Join(
                    DbContext.BdsmTestOrgItems.Include(i => i.Parent).ThenInclude(r => r.User),
                    x => x.Id,
                    y => y.Parent.Id,
                    (_, r) => r
                );

            if (greaterThanFilters.IsEmpty && lowerThanFilters.IsEmpty)
            {
                relevantItems = relevantItems.Where(i => i.Score > Settings.StrongTraitThreshold);
            }
            else
            {
                relevantItems = relevantItems.Where(i => i.Score > 0);
            }

            foreach (var trait in traitsToShow)
            {
                resultsDict[trait.GetDisplayName()] = await relevantItems
                    .Where(i => i.Trait == trait)
                    .OrderByDescending(row => row.Score)
                    .ThenBy(row => row.Parent.SubmittedAt)
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
                var displayName = await UsersService.GetDisplayNameAsync(testResult.UserId);
                if (displayName == null)
                    continue;

                testResult.UserDisplayName = displayName;
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
            var user = await DbContext.GetOrCreateUserEntityAsync(Context.Message.Author);

            if (testResult.Gender != Gender.Unspecified)
                user.Gender = testResult.Gender;

            var testResultDb = new BdsmTestOrgResult
            {
                SubmittedAt = testResult.Date,
                User = user,
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

        [Command("consent")]
        [Summary("Vypíše stav souhlasu s používáním obrázkových BDSM příkazů.")]
        public async Task ShowConsentAsync([Name("koho")] IUser target = null)
        {
            if (target is null)
                target = Context.User;

            var userEntity = await DbContext.GetOrCreateUserEntityAsync(target);
            var status = userEntity.HasGivenConsentTo(CommandConsent.BdsmImageCommands);
            var message = status ? Settings.ConsentRegistered : Settings.ConsentNotRegistered;
            await ReplyAsync(string.Format(message, Format.Sanitize(await UsersService.GetDisplayNameAsync(target))));
        }

        [Command("consent grant")]
        [Summary("Udělí souhlas s používáním obrázkových BDSM příkazů.")]
        public Task GrantConsentAsync()
            => UpdateConsentAsync(c => c | CommandConsent.BdsmImageCommands);

        [Command("consent revoke")]
        [Summary("Odvolá souhlas s používáním obrázkových BDSM příkazů.")]
        public Task RevokeConsentAsync()
            => UpdateConsentAsync(c => c & ~CommandConsent.BdsmImageCommands);

        private async Task UpdateConsentAsync(Func<CommandConsent, CommandConsent> consentUpdaterFunc)
        {
            await DbContext.UpdateCommandConsentAsync(Context.User, consentUpdaterFunc);
            await Context.Message.AddReactionAsync(ReactionSettings.Checkmark);
        }
    }
}
