using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Discord;
using Inkluzitron.Models;

namespace Inkluzitron.Modules.RicePurity
{
    public class RicePurityEmbed : EmbedBuilder
    {
        private readonly static NumberFormatInfo NumberFormat = new CultureInfo("cs-CZ").NumberFormat;

        public RicePurityEmbed WithBoard(
            List<RicePurityLeaderboardData> board, IUser user, int count, int start = 0, int limit = 10)
        {
            WithAuthor("Ricepurity žebříček", user.GetAvatarUrl());
            WithTimestamp(DateTime.Now);
            WithColor(new Color(241, 190, 223));

            var to = start + limit;
            if (to > count) to = count;
            WithFooter($"{start + 1}-{to} / {count}");

            var position = new StringBuilder();
            var users = new StringBuilder();
            var points = new StringBuilder();

            foreach (var item in board)
            {
                position.Append("**").Append(item.Position).AppendLine(".**");
                users.AppendLine(item.Name == null ? "*neznámý*" : Format.Sanitize(item.Name));
                points.AppendLine(item.Score.ToString("N0", NumberFormat));
            }

            if (board.Count > 0)
            {
                AddField("#", position.ToString(), true);
                AddField("Uživatel", users.ToString(), true);
                AddField("Skóre", points.ToString(), true);
            }

            return this;
        }
    }
}
