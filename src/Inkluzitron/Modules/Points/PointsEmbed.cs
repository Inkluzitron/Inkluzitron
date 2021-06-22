using Discord;
using Discord.WebSocket;
using Inkluzitron.Data.Entities;
using Inkluzitron.Extensions;
using Inkluzitron.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;

namespace Inkluzitron.Modules.Points
{
    public class PointsEmbed : EmbedBuilder
    {
        static private readonly NumberFormatInfo NumberFormat = new CultureInfo("cs-CZ").NumberFormat;

        private UsersService UsersService { get; }

        public PointsEmbed(UsersService usersService)
        {
            UsersService = usersService;
        }

        public async Task<PointsEmbed> WithBoard(
            Dictionary<int, User> board, DiscordSocketClient client,
            IUser user, int count, int start = 0, int limit = 10)
        {
            WithAuthor("Žebříček bodů", user.GetAvatarUrl());
            WithTimestamp(DateTime.Now);
            WithColor(new Color(241, 190, 223));

            var to = start + limit;
            if (to > count) to = count;
            WithFooter($"{start + 1}-{to} / {count}");

            this.WithMetadata(new PointsEmbedMetadata
            {
                Start = start
            });

            var position = new StringBuilder();
            var users = new StringBuilder();
            var points = new StringBuilder();

            foreach (var item in board)
            {
                position.Append("**").Append(item.Key).AppendLine(".**");
                var userData = client.GetUser(item.Value.Id);
                users.AppendLine(userData == null ? "*neznámý*" : Format.Sanitize(await UsersService.GetDisplayNameAsync(userData)));

                points.AppendLine(item.Value.GetTotalPoints().ToString("N0", NumberFormat));
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
