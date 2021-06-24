using Discord;
using Inkluzitron.Data.Entities;
using Inkluzitron.Extensions;
using Inkluzitron.Models.Settings;
using System;
using System.Linq;

namespace Inkluzitron.Modules.BdsmTestOrg
{
    static public class EmbedBuilderExtensions
    {
        static public EmbedBuilder WithBdsmTestOrgQuizInvitation(this EmbedBuilder builder, BdsmTestOrgSettings settings, IUser user)
        {
            return builder.WithTitle(settings.TestLinkUrl)
                .WithColor(new Color(0, 0, 0))
                .WithUrl(settings.TestLinkUrl)
                .WithDescription(settings.NoResultsOnRecordMessage)
                .WithFooter("BDSMTest.org");
        }

        static public EmbedBuilder WithBdsmTestOrgQuizResult(this EmbedBuilder builder, BdsmTestOrgSettings settings, BdsmTestOrgResult quizResult)
        {
            if (quizResult is null)
                throw new ArgumentNullException(nameof(quizResult));

            builder.WithTitle(quizResult.Link)
                .WithColor(new Color(0, 0, 0))
                .WithUrl(quizResult.Link)
                .WithTimestamp(quizResult.SubmittedAt)
                .WithFooter("BDSMTest.org");

            var relevantItems = quizResult.Items
                .Where(i => i.Score >= settings.StrongTraitThreshold)
                .OrderByDescending(i => i.Score)
                .ToList();

            if (relevantItems.Count == 0)
                builder.WithDescription(settings.NoTraitsToReportMessage);

            foreach (var relevantItem in relevantItems)
                builder.AddField(relevantItem.Trait.GetDisplayName(), $"{relevantItem.Score:P0}");

            return builder;
        }
    }
}
