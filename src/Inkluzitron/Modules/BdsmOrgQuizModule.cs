using Discord;
using Discord.Commands;
using Discord.Commands.Builders;
using Discord.WebSocket;
using Inkluzitron.Data;
using Inkluzitron.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Inkluzitron.Modules
{
    public class ArrayLengthComparer : IComparer<string[]>
    {
        public int Compare(string[] x, string[] y)
        {
            var comparison = y.Length.CompareTo(x.Length);
            var i = 0;

            while (comparison == 0)
                comparison = StringComparer.Ordinal.Compare(y[i], x[i]);

            return comparison;
        }
    }

    public class PaginationSettings
    {
        public string First { get; }
        public string Previous { get; }
        public string Next { get; }
        public string Last { get; }

        public IEmote[] Emojis { get; }
        public HashSet<string> Values { get; }
        public PaginationSettings(IConfiguration config)
        {
            First = config["Pagination:First"];
            Previous = config["Pagination:Previous"];
            Next = config["Pagination:Next"];
            Last = config["Pagination:Last"];

            Emojis = new[] { First, Previous, Next, Last }.Select(x => new Emoji(x)).ToArray<IEmote>();
            Values = Emojis.Select(e => e.Name).ToHashSet();
        }
    }

    public class BdsmTestOrgQuizModule : ModuleBase
    {
        private readonly SortedDictionary<string[], Subcommand> _lookupTable = new(new ArrayLengthComparer());
        private readonly Subcommand _fallbackSubcommand;
        private readonly string _subcommandsUsage;

        private readonly BotDatabaseContext _dbContext;
        private readonly PaginationSettings _settings;

        public BdsmTestOrgQuizModule(IServiceProvider serviceProvider)
        {
            _dbContext = serviceProvider.GetRequiredService<BotDatabaseContext>();
            _settings = serviceProvider.GetRequiredService<PaginationSettings>();

            var subcommandTypes = new[] { typeof(ProcessQuizSubmissionSubcommand), typeof(FallbackSubcommand), typeof(ShowLatestQuizResultSubcommand) };
            var usageBuilder = new StringBuilder();

            foreach (var subcommand in subcommandTypes.Select(t => ActivatorUtilities.CreateInstance(serviceProvider, t)).Cast<Subcommand>())
            {
                foreach (var alias in subcommand.Aliases)
                    _lookupTable.Add(alias, subcommand);

                if (subcommand.IsFallbackSubcommand)
                {
                    if (_fallbackSubcommand is null)
                        _fallbackSubcommand = subcommand;
                    else
                        throw new InvalidOperationException("More than one fallback subcommand specified.");
                }

                // todo: always joined, no point
                usageBuilder.AppendFormat("- {0}", string.Join(", ", subcommand.Aliases.Select(a => string.Join(" ", a)).OrderBy(x => x, StringComparer.Ordinal)));
                usageBuilder.AppendLine();

                usageBuilder.AppendFormat("  {0}", subcommand.Summary);
                usageBuilder.AppendLine();
            }

            _subcommandsUsage = usageBuilder.ToString();

            var dcClient = serviceProvider.GetRequiredService<DiscordSocketClient>();
            dcClient.ReactionAdded += async (x, y, z) => await Client_ReactionAdded(dcClient.CurrentUser.Id, x, y, z); // todo: -=
        }


        private async Task Client_ReactionAdded(ulong ownId, Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            if (arg3.Message.GetValueOrDefault() is not SocketUserMessage message)
                return;

            if (arg3.User.GetValueOrDefault() is not IUser user)
                return;

            if (user.Id == ownId)
                return;

            if (message.Author.Id != ownId)
                return;

            if (!_settings.Values.Contains(arg3.Emote.Name))
                return;

            if (message.Embeds.Count != 1)
                return;

            var embed = message.Embeds.Single();
            if (embed.Footer is not EmbedFooter footer)
                return;

            if (embed.Author is not EmbedAuthor author)
                return;

            async Task<BdsmTestOrgQuizResult> Navigate(BdsmTestOrgQuizResult submission, string navigationReaction)
            {
                var r = _dbContext.BdsmTestOrgQuizResults.Include(x => x.Items).AsAsyncEnumerable();

                if (navigationReaction == _settings.First) // the most recent, with maximum timestamp in DateTimeOffset
                    return await r.Where(x => x.SubmittedById == submission.SubmittedById).OrderByDescending(x => x.SubmittedAt).FirstOrDefaultAsync();
                else if (navigationReaction == _settings.Last) // the least recent, with minimum timestamp in DateTimeOffset
                    return await r.Where(x => x.SubmittedById == submission.SubmittedById).OrderBy(x => x.SubmittedAt).FirstOrDefaultAsync();
                else if (navigationReaction == _settings.Previous) // from all submissions whose timestamp is greater than the original, pick the closest (= previous) aka lowest one
                    return await r.Where(x => x.SubmittedById == submission.SubmittedById && x.SubmittedAt > submission.SubmittedAt).OrderBy(x => x.SubmittedAt).FirstOrDefaultAsync();
                else if (navigationReaction == _settings.Next) // from all submissions whose timestamp is lower than the original, pick the closest (= next) aka greatest one
                    return await r.Where(x => x.SubmittedById == submission.SubmittedById && x.SubmittedAt < submission.SubmittedAt).OrderByDescending(x => x.SubmittedAt).FirstOrDefaultAsync(); 
                else
                    return null;
            }

            if (!BdsmQuizIdentifier.TryParse(footer.Text, out var quizId))
                await message.ModifyAsync(p => p.Embed = BdsmQuizEmbedBuilder.BuildErrorEmbed());
            else if (await _dbContext.BdsmTestOrgQuizResults.FindAsync(quizId.Id) is not BdsmTestOrgQuizResult currentSubmission)
                await message.ModifyAsync(p => p.Embed = BdsmQuizEmbedBuilder.BuildUnableToLocateEmbed());
            else if (await Navigate(currentSubmission, arg3.Emote.Name) is BdsmTestOrgQuizResult newSubmission)
            {
                var newEmbedBuilder = new BdsmQuizEmbedBuilder(newSubmission);
                var newEmbed = await newEmbedBuilder.BuildAsync(_dbContext, x => x.WithAuthor(author.Name, author.IconUrl, author.Url));
                await message.ModifyAsync(p => p.Embed = newEmbed);
            }

            await message.RemoveReactionAsync(arg3.Emote, user);
        }

        private abstract class Subcommand
        {
            public string Summary { get; }
            public IEnumerable<string[]> Aliases { get; }
            public bool IsFallbackSubcommand { get; }

            protected Subcommand(string summary, bool isFallbackSubcommand = false, params string[] aliases)
            {
                Summary = summary;
                Aliases = aliases.Select(a => a.Split(" ")).ToArray();
                IsFallbackSubcommand = isFallbackSubcommand;
            }

            // TODO: do this properly somehow
            protected Task<IUserMessage> ReplyAsync(SocketCommandContext context, string message = null, bool isTTS = false, Embed embed = null, AllowedMentions allowedMentions = null)
            {
                var options = RequestOptions.Default;
                var reference = new MessageReference(context.Message.Id, context.Channel.Id, context.Guild.Id);

                if (allowedMentions == null)
                {
                    // Override default behaviour. Mention only replied user
                    allowedMentions = new AllowedMentions
                    {
                        MentionRepliedUser = true
                    };
                }

                return context.Message.ReplyAsync(message, isTTS, embed, allowedMentions, options);
            }

            public abstract Task HandleAsync(SocketCommandContext context);
        }

        private class ProcessQuizSubmissionSubcommand : Subcommand
        {
            private string TestResultAddedReaction { get; }

            private Regex TestResultRegex = new Regex(
                @"==\sResults\sfrom\sbdsmtest.org\s==\s+
    (?<results>.+)
    (?<link>https?://bdsmtest\.org/r/[\d\w]+)",
                RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline
            );

            private Regex TestResultItemRegex = new Regex(
                @"^(?<pctg>\d+)%\s+(?<trait>[^\n]+)$",
                RegexOptions.Multiline
            );
            private BotDatabaseContext DbContext { get; }

            public ProcessQuizSubmissionSubcommand(IConfiguration config, BotDatabaseContext dbContext)
                : base("Paste your test results to have them put on record.", aliases: "add")
            {

                TestResultAddedReaction = config["TestResultAddedReaction"];
                DbContext = dbContext;
            }

            public async override Task HandleAsync(SocketCommandContext context)
            {
                var reconstructedMessage = context.Message.ToString();
                var testResultMatches = TestResultRegex.Matches(reconstructedMessage);
                if (testResultMatches.Count == 0)
                    await ReplyAsync(context, "To mi nevychádza! Zkopíruj prosím celé výsledky, včetně hlavičky a odkazu na konci.");

                var testResultMatch = testResultMatches.Single();
                var testResultItems = testResultMatch.Groups["results"].Value;
                var testResultLink = testResultMatch.Groups["link"].Value;
                var itemsMatch = TestResultItemRegex.Matches(testResultItems);

                var testResult = new BdsmTestOrgQuizResult
                {
                    SubmittedAt = DateTimeOffset.Now,
                    SubmittedByName = context.Message.Author.Username,
                    SubmittedById = context.Message.Author.Id,
                    Link = testResultLink
                };

                foreach (Match m in itemsMatch)
                {
                    testResult.Items.Add(new QuizDoubleItem
                    {
                        Key = m.Groups["trait"].Value,
                        Value = ParseTraitPercentage(m.Groups["pctg"].Value)
                    });
                }

                await DbContext.BdsmTestOrgQuizResults.AddAsync(testResult);
                await DbContext.SaveChangesAsync();

                var emoji = Emote.Parse(TestResultAddedReaction);
                await context.Message.AddReactionAsync(emoji);
            }
        }

        private struct BdsmQuizIdentifier
        {
            public Guid Id { get; }

            private static readonly Regex Pattern = new Regex(@"^bdsmtest\.org test result ([0-9a-f]{32})", RegexOptions.IgnoreCase);

            public BdsmQuizIdentifier(Guid id)
            {
                Id = id;
            }

            public override string ToString()
                => $"bdsmtest.org test result {Id:n}";

            public static bool TryParse(string input, out BdsmQuizIdentifier id)
            {
                var match = Pattern.Match(input);
                var guid = Guid.Empty;

                if (match.Success)
                    guid = Guid.Parse(match.Groups[1].Value);

                id = new BdsmQuizIdentifier(guid);
                return match.Success;
            }
        }

        private class BdsmQuizEmbedBuilder
        {
            private readonly BdsmTestOrgQuizResult submission;

            public BdsmQuizEmbedBuilder(BdsmTestOrgQuizResult testResult)
            {
                this.submission = testResult;
            }

            public static Embed BuildUnableToLocateEmbed()
                => new EmbedBuilder()
                .WithColor(Color.Red)
                .AddField("Test Result Not Found", "The original test result no longer exists and as such, it is not possible to navigate from it.")
                .Build();

            public static Embed BuildErrorEmbed()
                => new EmbedBuilder()
                .WithColor(Color.Red)
                .AddField("Test Result Not Found", "The navigation metadata could not be processed.")
                .Build();

            public async Task<Embed> BuildAsync(BotDatabaseContext dbContext, Func<EmbedBuilder, EmbedBuilder> authorBuilder)
            {
                var count = await dbContext.BdsmTestOrgQuizResults
                  .AsAsyncEnumerable()
                  .Where(r => r.SubmittedById == submission.SubmittedById)
                  .CountAsync();

                var countNewer = await dbContext.BdsmTestOrgQuizResults
                    .AsAsyncEnumerable()
                    .Where(r => r.SubmittedById == submission.SubmittedById)
                    .Where(r => r.SubmittedAt > submission.SubmittedAt)
                    .CountAsync();

                var identifier = new BdsmQuizIdentifier(submission.ResultId);

                var embedBuilder = new EmbedBuilder()
                    .WithTitle(submission.Link)
                    .WithUrl(submission.Link)
                    .WithTimestamp(submission.SubmittedAt)
                    .WithFooter($"{identifier} – {countNewer + 1}/{count}");

                embedBuilder = authorBuilder(embedBuilder);

                foreach (var item in submission.Items.OfType<QuizDoubleItem>().OrderByDescending(i => i.Value))
                    embedBuilder = embedBuilder.AddField(item.Key, $"{item.Value:P0}", true);

                return embedBuilder.Build();
            }
        }

        private class ShowLatestQuizResultSubcommand : Subcommand
        {
            public BotDatabaseContext DbContext { get; }
            public PaginationSettings PaginationSettings { get; }

            public ShowLatestQuizResultSubcommand(BotDatabaseContext dbContext, PaginationSettings paginationSettings)
                : base("Show latest quiz result.", aliases: "show")
            {
                DbContext = dbContext;
                PaginationSettings = paginationSettings;
            }

            public override async Task HandleAsync(SocketCommandContext context)
            {
                var authorId = context.Message.Author.Id;

               var submission = await DbContext.BdsmTestOrgQuizResults
                    .Include(x => x.Items)
                    .AsAsyncEnumerable()
                    .Where(r => r.SubmittedById == authorId)
                    .OrderByDescending(r => r.SubmittedAt)
                    .Take(1)
                    .FirstOrDefaultAsync();

                if (submission == null)
                {
                    await ReplyAsync(context, "No quiz results on record.");
                    return;
                }

                var builder = new BdsmQuizEmbedBuilder(submission);
                var embed = await builder.BuildAsync(DbContext, x => x.WithAuthor(context.Message.Author));
                var message = await ReplyAsync(context, embed: embed);
                await message.AddReactionsAsync(PaginationSettings.Emojis);
            }
        }

        private class FallbackSubcommand : Subcommand
        {
            public FallbackSubcommand() : base("Prints usage and help.", isFallbackSubcommand: true)
            {

            }

            public override async Task HandleAsync(SocketCommandContext context)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Head over to http://www.bdsmtest.org and fill out the questionnare.");
                sb.AppendLine("Other commands:");

                sb.AppendLine("- $bdsmtest add <text results>");
                sb.AppendLine("  Record your test results.");

                await ReplyAsync(context, sb.ToString());
            }
        }

        [Command("bdsmtest")]
        [Summary("Handles the processing of BDSMTest.org results.")]
        public async Task HandleCommandAsync(params string[] args)
        {
            foreach ((var invocationKeywords, var subcommand) in _lookupTable)
            {
                if (!invocationKeywords.SequenceEqual(args.Take(invocationKeywords.Length)))
                    continue;

                await subcommand.HandleAsync(Context); // TODO: possibly error handling here
                return;
            }

            if (_fallbackSubcommand != null)
                await _fallbackSubcommand.HandleAsync(Context);
            else
                await ReplyAsync($"Takhle ne!{Environment.NewLine}{_subcommandsUsage}");                
        }

        private static double ParseTraitPercentage(string traitPercentage)
        {
            var pctg = int.Parse(traitPercentage, CultureInfo.InvariantCulture);
            if (pctg > 100)
                pctg = 100;
            else if (pctg < 0)
                pctg = 0;

            return pctg / 100.0;
        }        
    }
}
