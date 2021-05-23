using Discord;
using Discord.WebSocket;
using Inkluzitron.Data;
using Inkluzitron.Data.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Inkluzitron.Services
{
    public class PointsService
    {
        private DatabaseFactory DatabaseFactory { get; }
        private DiscordSocketClient DiscordClient { get; }

        public PointsService(DatabaseFactory factory, DiscordSocketClient discordClient)
        {
            DatabaseFactory = factory;
            DiscordClient = discordClient;

            DiscordClient.ReactionAdded += OnReactionAddedAsync;
        }

        private Task OnReactionAddedAsync(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (channel is not SocketGuildChannel)
                return Task.CompletedTask; // Only server messages increments points.

            return IncrementAsync(reaction);
        }

        public async Task IncrementAsync(SocketReaction reaction)
        {
            var user = reaction.User.IsSpecified ? reaction.User.Value : await DiscordClient.Rest.GetUserAsync(reaction.UserId);

            using var context = DatabaseFactory.Create();
            var userEntity = await GetOrCreateUserEntityAsync(context, user.Id);

            if (!CanIncrementPoints(userEntity, true))
                return;

            userEntity.LastReactionPointsIncrement = DateTime.UtcNow;
            userEntity.Points += ThreadSafeRandom.Next(0, 10);
            await context.SaveChangesAsync();
        }

        public async Task IncrementAsync(SocketMessage message)
        {
            using var context = DatabaseFactory.Create();
            var userEntity = await GetOrCreateUserEntityAsync(context, message.Author.Id);

            if (!CanIncrementPoints(userEntity, false))
                return;

            userEntity.LastMessagePointsIncrement = DateTime.UtcNow;
            userEntity.Points += ThreadSafeRandom.Next(0, 25);
            await context.SaveChangesAsync();
        }

        static private bool CanIncrementPoints(User userEntity, bool isReaction)
        {
            var lastIncrement = isReaction ? userEntity.LastReactionPointsIncrement : userEntity.LastMessagePointsIncrement;
            if (lastIncrement == null)
                return true;

            var limit = isReaction ? 0.5f : 1.0f;
            return (DateTime.UtcNow - lastIncrement.Value).TotalMinutes >= limit;
        }

        static private async Task<User> GetOrCreateUserEntityAsync(BotDatabaseContext context, ulong userId)
        {
            var userEntity = await context.Users.FirstOrDefaultAsync(o => o.Id == userId);

            if (userEntity != null)
                return userEntity;

            userEntity = new User() { Id = userId };
            await context.AddAsync(userEntity);

            return userEntity;
        }
    }
}
