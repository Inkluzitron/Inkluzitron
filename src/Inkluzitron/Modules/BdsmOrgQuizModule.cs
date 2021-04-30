using Discord;
using Discord.Commands;
using Discord.Commands.Builders;
using Discord.WebSocket;
using Inkluzitron.Data;
using Inkluzitron.Extensions;
using Inkluzitron.Settings;
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
       

    public class BdsmTestOrgQuizModule : ModuleBase
    {
        private readonly SortedDictionary<string[], Subcommand> _lookupTable = new(new ArrayLengthComparer());
        private readonly Subcommand _fallbackSubcommand;
        private readonly string _subcommandsUsage;

        private readonly BotDatabaseContext _dbContext;

        public BdsmTestOrgQuizModule(IServiceProvider serviceProvider)
        {
            _dbContext = serviceProvider.GetRequiredService<BotDatabaseContext>();

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
                    SubmittedAt = DateTime.UtcNow,
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

        private class ShowLatestQuizResultSubcommand : Subcommand
        {
            public BotDatabaseContext DbContext { get; }
            public ReactionSettings ReactionSettings { get; }

            public ShowLatestQuizResultSubcommand(BotDatabaseContext dbContext, ReactionSettings reactionSettings)
                : base("Show latest quiz result.", aliases: "show")
            {
                DbContext = dbContext;
                ReactionSettings = reactionSettings;
            }

            public override async Task HandleAsync(SocketCommandContext context)
            {
                var authorId = context.Message.Author.Id;

                var quizResultsOfUser = DbContext.BdsmTestOrgQuizResults
                    .Include(x => x.Items)
                    .Where(x => x.SubmittedById == authorId);

                var resultCount = await quizResultsOfUser.CountAsync();
                var mostRecentResult = await quizResultsOfUser.OrderByDescending(r => r.SubmittedAt)
                    .Take(1)
                    .FirstOrDefaultAsync();

                if (mostRecentResult is null)
                {
                    await ReplyAsync(context, "No quiz results on record.");
                    return;
                }

                var embed = new BdsmQuizEmbedBuilder()
                    .WithQuizResult(mostRecentResult, 1, resultCount)
                    .WithAuthor(context.Message.Author)
                    .Build();

                var message = await ReplyAsync(context, embed: embed);
                await message.AddReactionsAsync(ReactionSettings.PaginationReactions);
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
