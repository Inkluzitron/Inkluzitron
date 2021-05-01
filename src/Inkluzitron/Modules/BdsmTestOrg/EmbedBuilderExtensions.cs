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
        private const double TraitDisplayThreshold = 0.4;

        static public EmbedBuilder WithBdsmTestOrgQuizInvitation(this EmbedBuilder builder, IUser user, BdsmTestOrgSettings settings)
        {
            builder.WithTitle(settings.TestLinkUrl);
            builder.WithUrl(settings.TestLinkUrl);
            builder.WithDescription(settings.NoResultsOnRecordMessage);
            builder.WithFooter("0/0");
            builder.WithMetadata(new QuizEmbedMetadata
            {
                ResultId = 0,
                UserId = user.Id,
                PageNumber = 0
            });

            return builder;
        }

        static public EmbedBuilder WithBdsmTestOrgQuizResult(this EmbedBuilder builder, BdsmTestOrgQuizResult quizResult, int pageNumber, int pageCount)
        {
            if (quizResult is null)
                throw new ArgumentNullException(nameof(quizResult));

            builder.WithTitle(quizResult.Link);
            builder.WithUrl(quizResult.Link);
            builder.WithTimestamp(quizResult.SubmittedAt);
            builder.WithFooter($"{pageNumber}/{pageCount}");
            builder.WithMetadata(new QuizEmbedMetadata
            {
                ResultId = quizResult.ResultId,
                UserId = quizResult.SubmittedById,
                PageNumber = pageNumber
            });

            foreach (var item in quizResult.Items.OfType<QuizDoubleItem>().Where(i => i.Value > TraitDisplayThreshold).OrderByDescending(i => i.Value))
                builder.AddField(item.Key, $"{item.Value:P0}");

            return builder;
        }
    }
}
