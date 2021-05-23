using Discord;
using Discord.WebSocket;
using Inkluzitron.Data;
using Inkluzitron.Data.Entities;
using Inkluzitron.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using SysDraw = System.Drawing;

namespace Inkluzitron.Services
{
    public class PointsService
    {
        private DatabaseFactory DatabaseFactory { get; }
        private DiscordSocketClient DiscordClient { get; }
        private ProfilePictureService ProfilePictureService { get; }

        public PointsService(DatabaseFactory factory, DiscordSocketClient discordClient, ProfilePictureService profilePicture)
        {
            DatabaseFactory = factory;
            DiscordClient = discordClient;
            ProfilePictureService = profilePicture;

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
            var userEntity = await context.Users.AsQueryable().FirstOrDefaultAsync(o => o.Id == userId);

            if (userEntity != null)
                return userEntity;

            userEntity = new User() { Id = userId };
            await context.AddAsync(userEntity);

            return userEntity;
        }

        static private async Task<int> CalculatePositionAsync(BotDatabaseContext context, IUser user)
        {
            var users = await context.Users.AsQueryable()
                .Where(o => o.Points > 0)
                .ToListAsync();

            return users.FindIndex(o => o.Id == user.Id) + 1;
        }

        public async Task<SysDraw.Image> GetPointsAsync(IUser user)
        {
            using var context = DatabaseFactory.Create();
            var userEntity = await context.Users.AsQueryable().FirstOrDefaultAsync(o => o.Id == user.Id);

            if (userEntity == null)
                return null;

            const string font = "DejaVu Sans";
            using var positionFont = new Font(font, 45F);
            using var nicknameFont = new Font(font, 40F);
            using var titleTextFont = new Font(font, 20F);
            using var whiteBrush = new SolidBrush(SysDraw.Color.White);
            using var lightGrayBrush = new SolidBrush(SysDraw.Color.LightGray);

            var position = await CalculatePositionAsync(context, user);

            var bitmap = new Bitmap(1000, 300);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            graphics.RenderRectangle(new Rectangle(0, 0, bitmap.Width, bitmap.Height), SysDraw.Color.FromArgb(35, 39, 42), 15);
            graphics.RenderRectangle(new Rectangle(50, 50, 900, 200), SysDraw.Color.FromArgb(100, 0, 0, 0), 15);

            using var profilePicture = await ProfilePictureService.GetProfilePictureAsync(user);
            graphics.DrawImage(profilePicture, 70, 70, 160, 160);

            var positionTextSize = graphics.MeasureString($"#{position}", positionFont);
            graphics.DrawString("BODY", titleTextFont, whiteBrush, new PointF(250, 180));
            graphics.DrawString(userEntity.Points.ToString(), positionFont, lightGrayBrush, new PointF(340, 150));
            var positionTitleTextSize = graphics.MeasureString("POZICE", titleTextFont);
            graphics.DrawString("POZICE", titleTextFont, whiteBrush, new PointF(900 - positionTextSize.Width - positionTitleTextSize.Width, 180));
            graphics.DrawString($"#{position}", positionFont, whiteBrush, new PointF(910 - positionTextSize.Width, 150));

            var nickname = user.GetDisplayName().Cut(20);
            graphics.DrawString(nickname, nicknameFont, whiteBrush, new PointF(250, 60));

            return bitmap;
        }
    }
}
