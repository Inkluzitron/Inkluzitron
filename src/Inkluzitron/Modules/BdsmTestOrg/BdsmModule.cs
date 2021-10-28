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
using System.Diagnostics;
using System.IO;
using ImageMagick;

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
        private ImagesService ImagesService { get; }

        public BdsmModule(DatabaseFactory databaseFactory,
            ReactionSettings reactionSettings, BdsmTestOrgSettings bdsmTestOrgSettings,
            GraphPaintingService graphPainter, IHttpClientFactory factory,
            UserBdsmTraitsService bdsmTraitsService,
            BdsmGraphPaintingStrategy graphPaintingStrategy, UsersService usersService, ImagesService imagesService)
        {
            DatabaseFactory = databaseFactory;
            ReactionSettings = reactionSettings;
            Settings = bdsmTestOrgSettings;
            HttpClientFactory = factory;
            BdsmTraitsService = bdsmTraitsService;
            GraphPaintingService = graphPainter;
            GraphPaintingStrategy = graphPaintingStrategy;
            UsersService = usersService;
            ImagesService = imagesService;
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
        [Summary("Zobrazí svůj výsledek nebo výsledek uživatele.")]
        public async Task ShowUserResultAsync([Name("kdo")] IUser target = null)
        {
            if (target is null)
                target = Context.Message.Author;

            var quizResult = await DbContext.BdsmTestOrgResults.AsQueryable()
                .Include(x => x.Items)
                .Where(x => x.UserId == target.Id)
                .OrderByDescending(r => r.SubmittedAt)
                .FirstOrDefaultAsync();

            var embedBuilder = new EmbedBuilder().WithAuthor(target);

            embedBuilder = quizResult is null
                ? embedBuilder.WithBdsmTestOrgQuizInvitation(Settings, target)
                : embedBuilder.WithBdsmTestOrgQuizResult(Settings, quizResult);

            await ReplyAsync(embed: embedBuilder.Build());
        }

        [Command("remove")]
        [Alias("delete")]
        [Summary("Odstraní svůj výsledek z databáze.")]
        public async Task RemoveResultAsync()
        {
            await RemoveAllResultsAsync();
            await DbContext.SaveChangesAsync();
            await Context.Message.AddReactionAsync(ReactionSettings.Checkmark);
        }

        private async Task RemoveAllResultsAsync()
        {
            var target = Context.Message.Author;

            var quizResults = await DbContext.BdsmTestOrgResults.AsQueryable()
                .Where(x => x.UserId == target.Id)
                .ToArrayAsync();

            DbContext.BdsmTestOrgResults.RemoveRange(quizResults);
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

            await RemoveAllResultsAsync();
            await DbContext.BdsmTestOrgResults.AddAsync(testResultDb);
            await DbContext.SaveChangesAsync();
            await Context.Message.AddReactionAsync(ReactionSettings.BdsmTestResultAdded);

            await ShowUserResultAsync();
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

            await ReplyAsync(lastCheck.ToStringWithTraitInfluenceTable());
        }

        [Command("roll influence")]
        [Alias("tabulka")]
        [Summary("Vykreslí tabulku kdo (řádky) proti komu (sloupce) může používat spicy obrázkové příkazy, případně s jakou hodnotou kostek.")]
        public async Task ComputeAndDrawRollInfluenceAsync()
        {
            await using var _ = await DisposableReaction.CreateAsync(Context.Message, ReactionSettings.Loading, Context.Client.CurrentUser);
            var allTests = await DbContext.BdsmTestOrgResults.Include(r => r.User).Include(r => r.Items).ToListAsync();
            allTests.RemoveAll(t => !t.User.HasGivenConsentTo(CommandConsent.BdsmImageCommands));

            var scoreOfUserAgainstAll = new Dictionary<ulong, Dictionary<ulong, BdsmTraitOperationCheck>>();

            foreach (var userTest in allTests)
            {
                var row = new Dictionary<ulong, BdsmTraitOperationCheck>();

                foreach (var targetTest in allTests)
                {
                    var check = new BdsmTraitOperationCheck(Settings, BdsmTraitsService.CheckTranslations, Gender.Unspecified, Gender.Unspecified)
                    {
                        UserDisplayName = userTest.User.Name,
                        TargetDisplayName = targetTest.User.Name
                    };

                    UserBdsmTraitsService.CalculateTraitOperation(check, userTest, targetTest, 1);
                    row[targetTest.UserId] = check;
                }

                scoreOfUserAgainstAll[userTest.UserId] = row;
            }

            using var avatars = new ValuesDisposingDictionary<ulong, IMagickImage<byte>>();
            foreach (var userId in allTests.Select(x => x.UserId).Distinct())
            {
                using var rawAvatar = await ImagesService.GetAvatarAsync(Context.Guild, userId);
                var avatar = rawAvatar.Frames[0].Clone();
                avatar.Resize(64, 64);
                avatar.RoundImage();
                avatars[userId] = avatar;
            }

            using var image = new MagickImage(MagickColors.Black, (avatars.Count + 1) * 100, (avatars.Count + 1) * 100);

            var i = 0;
            var powers = scoreOfUserAgainstAll.Select(
                kvp => (kvp.Key, kvp.Value
                    .Select(kvp => (kvp.Value.Result, kvp.Value.RequiredValue))
                    .Select(x => x.Result switch {
                        BdsmTraitOperationCheckResult.InCompliance => 0,
                        BdsmTraitOperationCheckResult.Self => 0,
                        _ => x.RequiredValue
                    })
                    .Sum())
            ).OrderBy(x => x.Item2).ToList();

            var users = powers.Select(p => p.Key);
            foreach (var source in users)
            {
                var avatar = avatars[source];
                var imgCoord = i + 1;
                image.Composite(avatar, 18 + imgCoord * 100, 18, CompositeOperator.Over);
                image.Composite(avatar, 18, 18 + imgCoord * 100, CompositeOperator.Over);

                var drawables = new Drawables().Density(100).StrokeColor(MagickColors.White).StrokeWidth(1).TextAlignment(TextAlignment.Center).Gravity(Gravity.Center).FontPointSize(30);

                drawables = drawables
                    .Line(imgCoord * 100, 0, imgCoord * 100, image.Height)
                    .Line(0, imgCoord * 100, image.Width, imgCoord * 100);

                var targetCoord = 1;
                foreach (var targetCheck in users.Select(userId => scoreOfUserAgainstAll[userId][source]))
                {
                    var txt = "???";
                    var bg = MagickColors.White;

                    switch (targetCheck.Result)
                    {
                        case BdsmTraitOperationCheckResult.InCompliance:
                            txt = "OK";
                            break;

                        case BdsmTraitOperationCheckResult.Self:
                            txt = "-";
                            break;

                        case BdsmTraitOperationCheckResult.RollSucceeded:
                        case BdsmTraitOperationCheckResult.RollFailed:
                            txt = targetCheck.RequiredValue.ToString();
                            var pctg = 1.0 * targetCheck.RequiredValue / targetCheck.RollMaximum;
                            bg = MagickColor.FromRgba((byte)Math.Ceiling(pctg * 255), (byte)Math.Ceiling((1 - pctg) * 255), 0, 100);
                            break;
                    }

                    drawables = drawables
                        .FillColor(bg)
                        .StrokeColor(bg)
                        .Text(imgCoord * 100 + 50, targetCoord * 100 + 50 + 15, txt);

                    targetCoord++;
                }

                drawables.Draw(image);
                i++;
            }

            using var tmpFile = new TemporaryFile("png");
            using (var stream = File.OpenWrite(tmpFile.Path))
                await image.WriteAsync(stream, MagickFormat.Png);

            await ReplyFileAsync(tmpFile.Path);
        }
    }
}
