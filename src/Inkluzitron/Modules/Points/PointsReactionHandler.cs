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
    public class PointsReactionHandler : IReactionHandler
    {
        private PointsService PointsService { get; }
        private DiscordSocketClient Client { get; }
        private ReactionSettings ReactionSettings { get; }
        private readonly int BoardPageLimit = 10;

        public PointsReactionHandler(PointsService pointsService, DiscordSocketClient client,
            ReactionSettings reactionSettings)
        {
            PointsService = pointsService;
            Client = client;
            ReactionSettings = reactionSettings;
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

            var count = await PointsService.GetUserCountAsync();
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
                var board = await PointsService.GetLeaderboardAsync(newStart, BoardPageLimit, metadata.DateFrom);

                var newEmbed = new PointsEmbed()
                    .WithBoard(board, context.Client.CurrentUser, count, newStart, BoardPageLimit, metadata.DateFrom)
                    .Build();

                await message.ModifyAsync(msg => msg.Embed = newEmbed);
            }

            if (!context.IsPrivate) // DMs have blocked removing reactions.
                await message.RemoveReactionAsync(reaction, user);
            return true;
        }
    }
}
