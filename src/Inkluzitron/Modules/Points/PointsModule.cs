using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Inkluzitron.Contracts;
using Inkluzitron.Extensions;
using Inkluzitron.Models.Settings;
using Inkluzitron.Services;
using Inkluzitron.Utilities;
using System.Linq;
using System.Threading.Tasks;
using SysDraw = System.Drawing;

namespace Inkluzitron.Modules.Points
{
    [Group("body")]
    [Name("Body")]
    [Summary("Body se počítají stejně jako u GrillBot. Za každou reakci uživatel obdrží 0 až 10 bodů, za zprávu 0 až 25 bodů. Po odeslání zprávy " +
        "bot počítá jedno minutový cooldown. U reakce je cooldown 30 vteřin.")]
    public class PointsModule : ModuleBase, IReactionHandler
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
        [Summary("Aktuální stav svých bodů nebo bodů jiného uživatele.")]
        public async Task GetPointsAsync([Name("uživatel")]IUser member = null)
        {
            if (member == null) member = Context.User;
            using var points = await PointsService.GetPointsAsync(member);

            if (points == null)
            {
                await ReplyAsync($"Uživatel `{member.GetDisplayName()}` ještě nemá žádné body.");
                return;
            }

            var tmpFile = new TemporaryFile("png");
            points.Save(tmpFile.Path, SysDraw.Imaging.ImageFormat.Png);

            await ReplyFileAsync(tmpFile.Path);
        }

        [Command("board")]
        [Alias("list", "top")]
        [Summary("Žebříček uživatelů s nejvíce body. Zobrazí žebříček kolem zadaného uživatele.")]
        public async Task GetLeaderboardAsync([Name("uživatel")] IUser user)
        {
            var pos = await PointsService.GetUserPosition(user);
            if(pos < 0)
                await ReplyAsync("Tento uživatel nemá žádný záznam o bodech.");
            else
                await GetLeaderboardAsync(pos);
        }

        [Command("board")]
        [Alias("list", "top")]
        [Summary("Žebříček uživatelů s nejvíce body.")]
        public async Task GetLeaderboardAsync([Name("počátek")]int start = 0)
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

        public async Task<bool> HandleReactionAddedAsync(IUserMessage message, IEmote reaction, IUser user)
        {
            var embed = message.Embeds.FirstOrDefault();
            if (embed == null || embed.Author == null || embed.Footer == null)
                return false; // Embed checks

            if (!ReactionSettings.PaginationReactions.Any(emote => emote.IsEqual(reaction)))
                return false; // Reaction check.

            if (message.ReferencedMessage == null)
                return false;

            if (!embed.TryParseMetadata<PointsEmbedMetadata>(out var metadata))
                return false; // Not a points board embed.

            var count = await PointsService.GetUserCount();
            if (count == 0)
                return false;

            int newStart = metadata.Start;
            if (reaction.IsEqual(ReactionSettings.MoveToFirst))
                newStart = 0;
            else if (reaction.IsEqual(ReactionSettings.MoveToLast))
                newStart = count - 1;
            else if (reaction.IsEqual(ReactionSettings.MoveToNext))
                newStart += BoardPageLimit;
            else if (reaction.IsEqual(ReactionSettings.MoveToPrevious))
                newStart -= BoardPageLimit;

            if (newStart >= count)
                newStart = count - 1;
            else if (newStart < 0)
                newStart = 0;

            newStart -= newStart % BoardPageLimit;

            var context = new CommandContext(Client, message.ReferencedMessage);

            if (newStart != metadata.Start)
            {
                var board = PointsService.GetLeaderboard(newStart, BoardPageLimit);

                var newEmbed = new PointsEmbed()
                    .WithBoard(board, Client, context.Client.CurrentUser, count, newStart, BoardPageLimit)
                    .Build();

                await message.ModifyAsync(msg => msg.Embed = newEmbed);
            }

            if (!context.IsPrivate) // DMs have blocked removing reactions.
                await message.RemoveReactionAsync(reaction, user);
            return true;
        }
    }
}
