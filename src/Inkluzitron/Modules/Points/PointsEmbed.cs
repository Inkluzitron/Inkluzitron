using Discord;
using Inkluzitron.Extensions;
using Inkluzitron.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Inkluzitron.Modules.Points
{
    public class PointsEmbed : EmbedBuilder
    {
        static private readonly NumberFormatInfo NumberFormat = new CultureInfo("cs-CZ").NumberFormat;

        public PointsEmbed WithBoard(
            List<PointsLeaderboardData> board, IUser user, int count, int start = 0, int limit = 10, DateTime? dateFrom = null)
        {
            WithAuthor(
                $"Žebříček bodů{(dateFrom.HasValue ? $" od {dateFrom.Value:dd. MM. yyyy}" : "")}",
                user.GetAvatarUrl());
            WithTimestamp(DateTime.Now);
            WithColor(new Color(241, 190, 223));

            var to = start + limit;
            if (to > count) to = count;
            WithFooter($"{start + 1}-{to} / {count}");

            this.WithMetadata(new PointsEmbedMetadata
            {
                Start = start,
                DateFrom = dateFrom
            });

            var position = new StringBuilder();
            var users = new StringBuilder();
            var points = new StringBuilder();

            foreach (var item in board)
            {
                position.Append("**").Append(item.Position).AppendLine(".**");
                users.AppendLine(item.UserDisplayName == null ? "*neznámý*" : Format.Sanitize(item.UserDisplayName));

                points.AppendLine(item.Points.ToString("N0", NumberFormat));
            }

            if (board.Count > 0)
            {
                AddField("#", position.ToString(), true);
                AddField("Uživatel", users.ToString(), true);
                AddField("Body", points.ToString(), true);
            }

            return this;
        }
    }
}
