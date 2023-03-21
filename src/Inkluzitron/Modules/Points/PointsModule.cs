using Discord;
using Discord.Commands;
using EnumArg = Inkluzitron.Enums.CommandArguments;
using Inkluzitron.Models;
using Inkluzitron.Models.Settings;
using Inkluzitron.Services;
using Inkluzitron.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Inkluzitron.Modules.Points
{
    [Group("body")]
    [Name("Body")]
    [Alias("points")]
    [Summary("Body se počítají podobně jako u GrillBot. Za každou zprávu uživatel obdrží 0 až 25 bodů, " +
             "za reakci uživatel obdrží 0 až 10 bodů a autorovi zprávy přidá 5 bodů. Po odeslání zprávy " +
             "bot počítá jedno minutový cooldown. U reakce je cooldown 30 vteřin.")]
    public class PointsModule : ModuleBase
    {
        private PointsService PointsService { get; }
        private GraphPaintingService GraphPaintingService { get; }
        private PointsGraphPaintingStrategy GraphPaintingStrategy { get; }
        private ReactionSettings ReactionSettings { get; }
        private UsersService UsersService { get; }
        private readonly int BoardPageLimit = 10;

        public PointsModule(PointsService pointsService, ReactionSettings reactionSettings, GraphPaintingService graphPaintingService, PointsGraphPaintingStrategy graphPaintingStrategy, UsersService usersService)
        {
            PointsService = pointsService;
            ReactionSettings = reactionSettings;
            GraphPaintingService = graphPaintingService;
            GraphPaintingStrategy = graphPaintingStrategy;
            UsersService = usersService;
        }

        [Command("")]
        [Alias("kde", "gde")]
        [Summary("Zobrazí aktuální stav svých bodů nebo bodů jiného uživatele.")]
        public async Task GetPointsAsync([Name("uživatel")] IUser member = null)
        {
            if (member == null)
                member = Context.User;

            if (member.IsBot)
            {
                await ReplyAsync($"Nelze zobrazit body pro bota {Format.Sanitize(await UsersService.GetDisplayNameAsync(member))} (botům se body nepočítají).");
                return;
            }

            using var points = await PointsService.GetPointsAsync(member);

            await ReplyFileAsync(points.Path);
        }

        [Command("board")]
        [Alias("list")]
        [Summary("Žebříček uživatelů s nejvíce body. Volitelně zobrazí žebříček kolem zadaného uživatele.")]
        public async Task GetLeaderboardAsync([Name("uživatel")] IUser user = null)
        {
            if (user == null)
            {
                await GetLeaderboardAsync(0);
                return;
            }

            var pos = await PointsService.GetUserPositionAsync(user);
            await GetLeaderboardAsync(pos);
        }

        [Command("board")]
        [Alias("list")]
        [Summary("Žebříček uživatelů s nejvíce body za poslední den/týden/měsíc. Volitelně zobrazí žebříček kolem zadaného uživatele.")]
        public async Task GetWeeklyLeaderboardAsync(EnumArg.TimeSpan span, [Name("uživatel")] IUser user = null)
        {
            var from = span switch
            {
                EnumArg.TimeSpan.Today => DateTime.Today,
                EnumArg.TimeSpan.Week => DateTime.Today.AddDays(-6),
                EnumArg.TimeSpan.Month => DateTime.Today.AddMonths(-1).AddDays(1),
                _ => throw new NotImplementedException()
            };

            if (user == null)
            {
                await GetLeaderboardAsync(0, from);
                return;
            }

            var pos = await PointsService.GetUserPositionAsync(user);
            await GetLeaderboardAsync(pos, from);
        }

        public async Task GetLeaderboardAsync(int start, DateTime? from = null)
        {
            var count = await PointsService.GetUserCountAsync();

            if (start < 1) start = 1;
            if (start >= count) start = count - 1;
            start -= start % BoardPageLimit;

            var board = await PointsService.GetLeaderboardAsync(start, BoardPageLimit, from);
            var embed = new PointsEmbed().WithBoard(
                board, Context.Client.CurrentUser, count, start, BoardPageLimit, from);

            var message = await ReplyAsync(embed: embed.Build());
            await message.AddReactionsAsync(ReactionSettings.PaginationReactions);
        }

        [Command("graph"), Alias("stats")]
        [Summary("Graf všech uživatelů a jimi získaných bodů.")]
        public async Task GetGraphAsync()
        {
            await using var _ = await DisposableReaction.CreateAsync(Context.Message, ReactionSettings.Loading, Context.Client.CurrentUser);
            var results = new Dictionary<string, IReadOnlyList<GraphItem>>
            {
                { "Body", await PointsService.GetUsersTotalPointsAsync() }
            };

            using var file = new TemporaryFile("png");
            using var graph = await GraphPaintingService.DrawAsync(Context.Guild, GraphPaintingStrategy, results);
            graph.Write(file.Path, ImageMagick.MagickFormat.Png);

            await ReplyFileAsync(file.Path);
        }
    }
}
