using Discord;
using Inkluzitron.Data;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Inkluzitron.Modules
{
    public class BdsmTestOrgQuizEmbedBuilder : EmbedBuilder
    {
        static private readonly Regex FooterPattern = new Regex(@"^BdsmTest\.org~([a-f0-9]+) – (\d+)", RegexOptions.IgnoreCase);
        private const double TraitDisplayThreshold = 0.5;

        static public bool TryParseFooterText(string footerText, out ulong userId, out int pageNumber)
        {
            var match = FooterPattern.Match(footerText);
            if (match.Success)
            {
                userId = Convert.ToUInt64(match.Groups[1].Value, 16);
                pageNumber = int.Parse(match.Groups[2].Value);
                return true;
            }
            else
            {
                userId = 0;
                pageNumber = 0;
                return false;
            }
        }

        public BdsmTestOrgQuizEmbedBuilder WithQuizResult(BdsmTestOrgQuizResult quizResult, int pageNumber, int pageCount)
        {
            if (quizResult is null)
                throw new ArgumentNullException(nameof(quizResult));

            WithTitle(quizResult.Link);
            WithUrl(quizResult.Link);
            WithTimestamp(quizResult.SubmittedAt);
            WithFooter($"BdsmTest.org~{quizResult.SubmittedById:X} – {pageNumber}/{pageCount}");

            foreach (var item in quizResult.Items.OfType<QuizDoubleItem>().OrderByDescending(i => i.Value).Where(i => i.Value > TraitDisplayThreshold))
                AddField(item.Key, $"{item.Value:P0}", true);

            return this;
        }
    }
}
