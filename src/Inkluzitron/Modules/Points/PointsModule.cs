using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Inkluzitron.Contracts;
using Inkluzitron.Extensions;
using Inkluzitron.Models.Settings;
using Inkluzitron.Services;
using System.Linq;
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
        private DiscordSocketClient Client { get; }
        private ReactionSettings ReactionSettings { get; }
        private readonly int BoardPageLimit = 10;

        public PointsModule(PointsService pointsService, DiscordSocketClient client,
            ReactionSettings reactionSettings)
        {
            PointsService = pointsService;
            Client = client;
            ReactionSettings = reactionSettings;
        }

        [Command("")]
        [Alias("kde", "gde")]
        [Summary("Zobrazí aktuální stav svých bodů.")]
        public async Task GetPointsAsync()
        {
            await GetPointsAsync(Context.User);
        }

        [Command("")]
        [Alias("kde", "gde")]
        [Summary("Zobrazí aktuální stav bodů jiného uživatele.")]
        public async Task GetPointsAsync([Name("uživatel")]IUser member)
        {
            using var points = await PointsService.GetPointsAsync(member);

            if (points == null)
            {
                await ReplyAsync($"Uživatel `{member.GetDisplayName()}` ještě nemá žádné body.");
                return;
            }

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
        public async Task GetLeaderboardAsync([Name("offset")]int start)
        {
            var count = await PointsService.GetUserCount();

            if (start >= count) start = count - 1;
            start -= start % BoardPageLimit;

            var board = PointsService.GetLeaderboard(start, BoardPageLimit);

            var embed = new PointsEmbed().WithBoard(
                board, Client, Context.Client.CurrentUser, count, start, BoardPageLimit);

            var message = await ReplyAsync(embed: embed.Build());
            await message.AddReactionsAsync(ReactionSettings.PaginationReactions);
        }

        [Command("board")]
        [Alias("list")]
        [Summary("Žebříček uživatelů s nejvíce body. Zobrazí žebříček kolem zadaného uživatele.")]
        public async Task GetLeaderboardAsync([Name("uživatel")] IUser user)
        {
            var pos = await PointsService.GetUserPosition(user);
            if (pos < 0)
                await ReplyAsync("Tento uživatel nemá žádný záznam o bodech.");
            else
                await GetLeaderboardAsync(pos);
        }
    }
}
