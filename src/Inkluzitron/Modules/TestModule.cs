using Discord.WebSocket;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Globalization;
using Inkluzitron.Data;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Microsoft.Extensions.Configuration;

namespace Inkluzitron.Modules
{
    public class TestResultSubmissionModule : ModuleBase
    {
        private IServiceProvider ServiceProvider { get; }
        private DiscordSocketClient DiscordClient { get; }

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

        private DataContext Db => ServiceProvider.GetRequiredService<DataContext>();

        public TestResultSubmissionModule(IServiceProvider serviceProvider, IConfiguration config, DiscordSocketClient discordClient)
        {
            ServiceProvider = serviceProvider;
            DiscordClient = discordClient;
            DiscordClient.MessageReceived += OnMessageReceivedAsync;
            TestResultAddedReaction = config["TestResultAddedReaction"];
        }

        private bool IsBotTagged(SocketMessage msg)
            => msg.MentionedRoles.Any(r => r.Tags.BotId == DiscordClient.CurrentUser.Id) ||
               msg.MentionedUsers.Any(u => u.IsBot && u.Id == DiscordClient.CurrentUser.Id);

        private static double ParseTraitPercentage(string traitPercentage)
        {
            var pctg = int.Parse(traitPercentage, CultureInfo.InvariantCulture);
            if (pctg > 100)
                pctg = 100;
            else if (pctg < 0)
                pctg = 0;

            return pctg / 100.0;
        }

        private async Task OnMessageReceivedAsync(SocketMessage msg)
        {
            if (!IsBotTagged(msg))
                return;

            var testResultMatch = TestResultRegex.Match(msg.ToString());
            if (!testResultMatch.Success)
                return;

            var testResultItems = testResultMatch.Groups["results"].Value;
            var testResultLink = testResultMatch.Groups["link"].Value;
            var itemsMatch = TestResultItemRegex.Matches(testResultItems);

            var testResult = new BorgTestResult
            {
                SubmittedAt = DateTimeOffset.Now,
                SubmittedByName = msg.Author.Username,
                SubmittedById = msg.Author.Id,
                Link = testResultLink
            };

            foreach (Match m in itemsMatch)
            {
                testResult.Items.Add(new DoubleTestResultItem
                {
                    Key = m.Groups["trait"].Value,
                    Value = ParseTraitPercentage(m.Groups["pctg"].Value)
                });
            }

            
            await Db.BorgTestResults.AddAsync(testResult);
            await Db.SaveChangesAsync();

            var emoji = Emote.Parse(TestResultAddedReaction);
            await msg.AddReactionAsync(emoji);
        }
    }
}
