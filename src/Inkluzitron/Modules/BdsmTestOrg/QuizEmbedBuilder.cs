using Discord;
using Inkluzitron.Data.Entities;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Inkluzitron.Modules.BdsmTestOrg
{
    public class QuizEmbedBuilder : EmbedBuilder
    {
        static private readonly Regex FooterPattern = new(@"^BdsmTest\.org~([a-f0-9]+) – (\d+)", RegexOptions.IgnoreCase);
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

        public QuizEmbedBuilder WithQuizResult(BdsmTestOrgQuizResult quizResult, int pageNumber, int pageCount)
        {
            if (quizResult is null)
                throw new ArgumentNullException(nameof(quizResult));

            WithTitle(quizResult.Link);
            WithUrl(quizResult.Link);
            WithTimestamp(quizResult.SubmittedAt);
            WithFooter($"BdsmTest.org~{quizResult.SubmittedById:X} – {pageNumber}/{pageCount}");

            foreach (var item in quizResult.Items.OfType<QuizDoubleItem>().Where(i => i.Value > TraitDisplayThreshold).OrderByDescending(i => i.Value))
                AddField(item.Key, $"{item.Value:P0}", true);

            return this;
        }
    }
}
