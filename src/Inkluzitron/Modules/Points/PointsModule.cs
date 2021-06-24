using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Inkluzitron.Extensions;
using Inkluzitron.Models;
using Inkluzitron.Models.Settings;
using Inkluzitron.Services;
using Inkluzitron.Utilities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Inkluzitron.Modules.Points
{
    [Group("body")]
    [Name("Body")]
    [Alias("points")]
    [Summary("Body se počítají stejně jako u GrillBot. Za každou reakci uživatel obdrží 0 až 10 bodů, za zprávu 0 až 25 bodů. Po odeslání zprávy " +
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
        [Summary("Zobrazí aktuální stav svých bodů.")]
        public async Task GetPointsAsync()
            => await GetPointsAsync(Context.User);

        [Command("")]
        [Alias("kde", "gde")]
        [Summary("Zobrazí aktuální stav bodů jiného uživatele.")]
        public async Task GetPointsAsync([Name("uživatel")] IUser member)
        {
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
        [Summary("Žebříček uživatelů s nejvíce body.")]
        public async Task GetLeaderboardAsync()
        {
            await GetLeaderboardAsync(0);
        }

        [Command("board")]
        [Alias("list")]
        [Summary("Žebříček uživatelů s nejvíce body s posunem od počátku tabulky.")]
        public async Task GetLeaderboardAsync([Name("offset")] int start)
        {
            var count = await PointsService.GetUserCountAsync();

            if (start < 1) start = 1;
            if (start >= count) start = count - 1;
            start -= start % BoardPageLimit;

            var board = await PointsService.GetLeaderboardAsync(start, BoardPageLimit);
            var embed = new PointsEmbed().WithBoard(
                board, Context.Client.CurrentUser, count, start, BoardPageLimit);

            var message = await ReplyAsync(embed: embed.Build());
            await message.AddReactionsAsync(ReactionSettings.PaginationReactions);
        }

        [Command("board")]
        [Alias("list")]
        [Summary("Žebříček uživatelů s nejvíce body. Zobrazí žebříček kolem zadaného uživatele.")]
        public async Task GetLeaderboardAsync([Name("uživatel")] IUser user)
        {
            var pos = await PointsService.GetUserPositionAsync(user);
            await GetLeaderboardAsync(pos);
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

        [Command("kachna")]
        [Alias("get kis", "z kachny")]
        [Summary("Synchronizuje body získané nákupem z kachničky.")]
        public async Task SynchronizeKisPointsAsync()
        {
            var message = await PointsService.SynchronizeKisPointsAsync(Context.User);
            await ReplyAsync(message);
        }
    }
}
