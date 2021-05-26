using Discord;
using Discord.WebSocket;
using Inkluzitron.Data;
using Inkluzitron.Data.Entities;
using Inkluzitron.Extensions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SysDraw = System.Drawing;

namespace Inkluzitron.Services
{
    public class PointsService : IDisposable
    {
        private DatabaseFactory DatabaseFactory { get; }
        private DiscordSocketClient DiscordClient { get; }
        private ImagesService ImagesService { get; }

        private Font PositionFont { get; }
        private Font NicknameFont { get; }
        private Font TitleTextFont { get; }
        private SolidBrush WhiteBrush { get; }
        private SolidBrush LightGrayBrush { get; }

        public PointsService(DatabaseFactory factory, DiscordSocketClient discordClient, ImagesService imagesService)
        {
            DatabaseFactory = factory;
            DiscordClient = discordClient;
            ImagesService = imagesService;

            DiscordClient.ReactionAdded += OnReactionAddedAsync;

            const string font = "Comic Sans MS";
            PositionFont = new Font(font, 45F);
            NicknameFont = new Font(font, 40F);
            TitleTextFont = new Font(font, 20F);
            WhiteBrush = new SolidBrush(SysDraw.Color.White);
            LightGrayBrush = new SolidBrush(SysDraw.Color.LightGray);
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

        public async Task AddPointsAsync(IUser user, int points, bool decrement = false)
        {
            await AddPointsAsync(user, 0, points, decrement);
        }

            public async Task AddPointsAsync(IUser user, int from, int to, bool decrement = false)
        {
            using var context = DatabaseFactory.Create();
            var userEntity = await GetOrCreateUserEntityAsync(context, user.Id);

            userEntity.Points += (decrement ? -1 : 1) * ThreadSafeRandom.Next(from, to);
            await context.SaveChangesAsync();
        }

        static private bool CanIncrementPoints(User userEntity, bool isReaction)
        {
            var lastIncrement = isReaction ? userEntity.LastReactionPointsIncrement : userEntity.LastMessagePointsIncrement;
            if (lastIncrement == null)
                return true;

            var limit = isReaction ? 0.5 : 1.0;
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

        public async Task<int> GetUserPosition(IUser user)
        {
            using var context = DatabaseFactory.Create();
            return await GetUserPosition(context, user);
        }

        public async Task<int> GetUserPosition(BotDatabaseContext context, IUser user)
        {
            var users = await context.Users.AsQueryable()
                .OrderByDescending(o => o.Points)
                .ToListAsync();

            return users.FindIndex(o => o.Id == user.Id) + 1;
        }

        public Dictionary<int, User> GetLeaderboard(int startFrom = 0, int count = 10)
        {
            using var context = DatabaseFactory.Create();
            var users = context.Users.AsQueryable()
                .OrderByDescending(u => u.Points)
                .Skip(startFrom).Take(count);

            var board = new Dictionary<int, User>();

            foreach (var user in users)
            {
                startFrom++;
                board.Add(startFrom, user);
            }

            return board;
        }

        public async Task<int> GetUserCount()
        {
            using var context = DatabaseFactory.Create();
            return await context.Users.AsQueryable().CountAsync();
        }

        public async Task<SysDraw.Image> GetPointsAsync(IUser user)
        {
            using var context = DatabaseFactory.Create();
            var userEntity = await context.Users.AsQueryable().FirstOrDefaultAsync(o => o.Id == user.Id);

            if (userEntity == null)
                return null;

            var position = await GetUserPosition(context, user);

            var bitmap = new Bitmap(1000, 300);
            using var graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            graphics.RenderRectangle(new Rectangle(0, 0, bitmap.Width, bitmap.Height), SysDraw.Color.FromArgb(35, 39, 42), 15);
            graphics.RenderRectangle(new Rectangle(50, 50, 900, 200), SysDraw.Color.FromArgb(100, 0, 0, 0), 15);

            using var profilePicture = (await ImagesService.GetAvatarAsync(user)).Frames[0];
            using var roundedImage = profilePicture.RoundImage();
            graphics.DrawImage(roundedImage, 70, 70, 160, 160);

            var positionTextSize = graphics.MeasureString($"#{position}", PositionFont);
            graphics.DrawString("BODY", TitleTextFont, WhiteBrush, new PointF(250, 180));
            graphics.DrawString(userEntity.Points.ToString(), PositionFont, LightGrayBrush, new PointF(340, 150));
            var positionTitleTextSize = graphics.MeasureString("POZICE", TitleTextFont);
            graphics.DrawString("POZICE", TitleTextFont, WhiteBrush, new PointF(900 - positionTextSize.Width - positionTitleTextSize.Width, 180));
            graphics.DrawString($"#{position}", PositionFont, WhiteBrush, new PointF(910 - positionTextSize.Width, 150));

            var nickname = user.GetDisplayName().Cut(20);
            graphics.DrawString(nickname, NicknameFont, WhiteBrush, new PointF(250, 60));

            return bitmap;
        }

        public void Dispose()
        {
            PositionFont.Dispose();
            NicknameFont.Dispose();
            TitleTextFont.Dispose();
            WhiteBrush.Dispose();
            LightGrayBrush.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
