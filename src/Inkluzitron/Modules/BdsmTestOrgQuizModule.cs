using Discord;
using Discord.Commands;
using Inkluzitron.Data;
using Inkluzitron.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Inkluzitron.Modules
{
    [Name("BDSM test")]
    public class BdsmTestOrgQuizModule : ModuleBase
    {
        static private readonly Regex TestResultRegex = new Regex(
            @"==\sResults\sfrom\sbdsmtest.org\s==\s+
    (?<results>.+)
    (?<link>https?://bdsmtest\.org/r/[\d\w]+)",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline
        );

        static private readonly Regex TestResultItemRegex = new Regex(
            @"^(?<pctg>\d+)%\s+(?<trait>[^\n]+?)\s*$",
            RegexOptions.Multiline
        );

        private BotDatabaseContext DbContext { get; }
        private ReactionSettings ReactionSettings { get; }
        private string NoResultsOnRecordMessage { get; }
        private string InvalidFormatMessage { get; }
        private string LinkAlreadyPresentMessage { get; }
        private string InvalidTraitMessage { get; }
        private string InvalidPercentageMessage { get; }
        private string InvalidTraitCountMessage { get; }
        private HashSet<string> TraitList { get; }

        public BdsmTestOrgQuizModule(BotDatabaseContext dbContext, ReactionSettings reactionSettings, IConfiguration config)
        {
            DbContext = dbContext;
            ReactionSettings = reactionSettings;

            const string SectionName = "BdsmTestOrgQuizModule";
            string GetRequiredConfig(string key)
            {
                key = $"{SectionName}:{key}";
                var result = config.GetValue<string>(key);
                return result ?? throw new InvalidOperationException($"Missing required configuration value with key {key}");
            }

            NoResultsOnRecordMessage = GetRequiredConfig(nameof(NoResultsOnRecordMessage));
            InvalidFormatMessage = GetRequiredConfig(nameof(InvalidFormatMessage));
            LinkAlreadyPresentMessage = GetRequiredConfig(nameof(LinkAlreadyPresentMessage));
            InvalidTraitMessage = GetRequiredConfig(nameof(InvalidTraitMessage));
            InvalidPercentageMessage = GetRequiredConfig(nameof(InvalidPercentageMessage));
            InvalidTraitCountMessage = GetRequiredConfig(nameof(InvalidTraitCountMessage));
            TraitList = new HashSet<string>(config.GetSection($"{SectionName}:Traits").GetChildren().Select(c => c.Value));
        }

        [Command("bdsmtest")]
        [Alias("bdsm")]
        [Summary("Zobrazí výsledky BdsmTest.org uživatele, nebo takový výsledek přidá do databáze.\nPro přidání vložte jako parametr váš výsledek. Pro získání nezadávejte žádný parametr.")]
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

            var resultCount = await quizResultsOfUser.CountAsync();
            var mostRecentResult = await quizResultsOfUser.OrderByDescending(r => r.SubmittedAt)
                .Take(1)
                .FirstOrDefaultAsync();

            if (mostRecentResult is null)
            {
                await ReplyAsync(NoResultsOnRecordMessage);
                return;
            }

            var embed = new BdsmTestOrgQuizEmbedBuilder()
                .WithQuizResult(mostRecentResult, 1, resultCount)
                .WithAuthor(Context.Message.Author)
                .Build();

            var message = await ReplyAsync(embed: embed);
            await message.AddReactionsAsync(ReactionSettings.PaginationReactions);
        }

        private async Task ProcessQuizResultSubmission()
        {
            var reconstructedMessage = Context.Message.ToString();
            var testResultMatches = TestResultRegex.Matches(reconstructedMessage);
            if (testResultMatches.Count == 0)
            {
                await ReplyAsync(InvalidFormatMessage);
                return;
            }

            var testResultMatch = testResultMatches.Single();
            var testResultItems = testResultMatch.Groups["results"].Value;
            var testResultLink = testResultMatch.Groups["link"].Value;

            if (await DbContext.BdsmTestOrgQuizResults.AsAsyncEnumerable().AnyAsync(r => r.Link == testResultLink))
            {
                await ReplyAsync(LinkAlreadyPresentMessage);
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
                if (!TraitList.Contains(traitName))
                {
                    await ReplyAsync(InvalidTraitMessage);
                    return;
                }

                if (!TryParseTraitPercentage(match.Groups["pctg"].Value, out var traitPercentage))
                {
                    await ReplyAsync(InvalidPercentageMessage);
                    return;
                }

                testResult.Items.Add(new QuizDoubleItem
                {
                    Key = traitName,
                    Value = traitPercentage
                });
            }

            if (testResult.Items.Count != TraitList.Count)
            {
                await ReplyAsync(InvalidTraitCountMessage);
                return;
            }

            await DbContext.BdsmTestOrgQuizResults.AddAsync(testResult);
            await DbContext.SaveChangesAsync();
            await Context.Message.AddReactionAsync(ReactionSettings.BdsmTestResultAdded);
        }

        static private bool TryParseTraitPercentage(string traitPercentage, out double percentage)
        {
            if (!int.TryParse(traitPercentage, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integralPercentage))
            {
                percentage = 0;
                return false;
            }
            else if (integralPercentage < 0 || integralPercentage > 100)
            {
                percentage = 0;
                return false;
            }

            percentage = integralPercentage / 100.0;
            return true;
        }        
    }
}
