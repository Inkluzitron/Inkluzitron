using Discord;
using Discord.WebSocket;
using GrapeCity.Documents.Imaging;
using Inkluzitron.Data;
using Inkluzitron.Data.Entities;
using Inkluzitron.Extensions;
using Inkluzitron.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using SysDraw = System.Drawing;

namespace Inkluzitron.Services
{
    public class PointsService : IDisposable
    {
        static private readonly DateTime FallbackDateTime = new(2000, 1, 1);
        static private readonly TimeSpan MessageIncrementCooldown = TimeSpan.FromSeconds(60);
        static private readonly TimeSpan ReactionIncrementCooldown = TimeSpan.FromSeconds(30);

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

        private async Task OnReactionAddedAsync(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (channel is not IGuildChannel)
                return; // Only server messages increments points.

            var user = reaction.User.IsSpecified
                ? reaction.User.Value
                : await DiscordClient.Rest.GetUserAsync(reaction.UserId);

            if (user.IsBot)
                return;

            await AddIncrementalPoints(
                user,
                ThreadSafeRandom.Next(0, 10),
                u => DateTime.UtcNow.Subtract(u.LastReactionPointsIncrement ?? FallbackDateTime) < ReactionIncrementCooldown,
                u => u.LastReactionPointsIncrement = DateTime.UtcNow
            );
        }

        public async Task IncrementAsync(SocketMessage message)
        {
            if (message.Author.IsBot)
                return;

            await AddIncrementalPoints(
                message.Author,
                ThreadSafeRandom.Next(0, 25),
                u => DateTime.UtcNow.Subtract(u.LastMessagePointsIncrement ?? FallbackDateTime) < MessageIncrementCooldown,
                u => u.LastMessagePointsIncrement = DateTime.UtcNow
            );
        }

        public async Task AddIncrementalPoints(IUser user, int amount, Func<User, bool> isOnCooldownFunc, Action<User> resetCooldownAction)
        {
            using var context = DatabaseFactory.Create();

            await Patiently.HandleDbConcurrency(async () =>
            {
                var userEntity = await GetOrCreateUserEntityAsync(context, user.Id);
                if (isOnCooldownFunc(userEntity))
                    return;

                userEntity.Points += amount;
                resetCooldownAction(userEntity);
                await context.SaveChangesAsync();
            });
        }

        public async Task AddPointsAsync(IUser user, int points, bool decrement = false)
        {
            using var context = DatabaseFactory.Create();

            await Patiently.HandleDbConcurrency(async () =>
            {
                var userEntity = await GetOrCreateUserEntityAsync(context, user.Id);
                userEntity.Points += (decrement ? -1 : 1) * points;
                await context.SaveChangesAsync();
            });
        }

        static private async Task<User> GetOrCreateUserEntityAsync(BotDatabaseContext context, ulong userId)
        {
            var userEntity = await context.Users.AsQueryable().FirstOrDefaultAsync(o => o.Id == userId);

            if (userEntity != null)
                return userEntity;

            userEntity = new User() { Id = userId };
            await context.AddAsync(userEntity);
            await context.SaveChangesAsync();

            return userEntity;
        }

        public async Task<int> GetUserPositionAsync(IUser user)
        {
            using var context = DatabaseFactory.Create();
            return await GetUserPositionAsync(context, user);
        }

        static public async Task<int> GetUserPositionAsync(BotDatabaseContext context, IUser user)
        {
            var index = await context.Users.AsQueryable()
                .Where(u => u.Id == user.Id)
                .Select(u => u.Points)
                .SelectMany(ownPoints => context.Users.AsQueryable().Where(u => u.Points > ownPoints))
                .CountAsync();

            return index + 1;
        }

        public async Task<Dictionary<int, User>> GetLeaderboardAsync(int startFrom = 0, int count = 10)
        {
            using var context = DatabaseFactory.Create();
            var users = context.Users.AsQueryable()
                .OrderByDescending(u => u.Points)
                .Skip(startFrom).Take(count)
                .AsAsyncEnumerable();

            var board = new Dictionary<int, User>();

            await foreach (var user in users)
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

        public async Task<TemporaryFile> GetPointsAsync(IUser user)
        {
            User userEntity;
            int position;

            using (var context = DatabaseFactory.Create())
            {
                userEntity = await GetOrCreateUserEntityAsync(context, user.Id);
                position = await GetUserPositionAsync(context, user);
            }

            using var profilePicture = await ImagesService.GetAvatarAsync(user);

            if (profilePicture.IsAnimated)
            {
                var tmpFile = new TemporaryFile("gif");
                using var gifWriter = new GcGifWriter(tmpFile.Path);
                using var gcBitmap = new GcBitmap();

                foreach (var frame in profilePicture.Frames)
                {
                    using var roundedProfilePicture = frame.RoundImage();
                    using var baseImage = RenderPointsBaseFrame(userEntity, position, user);

                    using var graphics = Graphics.FromImage(baseImage);
                    graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    graphics.DrawImage(roundedProfilePicture, 70, 70, 160, 160);

                    using var destinationStream = new MemoryStream();
                    baseImage.Save(destinationStream, SysDraw.Imaging.ImageFormat.Png);
                    destinationStream.Seek(0, SeekOrigin.Begin);

                    gcBitmap.Load(destinationStream);
                    gifWriter.AppendFrame(gcBitmap, disposalMethod: GifDisposalMethod.RestoreToBackgroundColor, delayTime: profilePicture.GifDelay.Value);
                }

                return tmpFile;
            }
            else
            {
                var baseImage = RenderPointsBaseFrame(userEntity, position, user);
                using var graphics = Graphics.FromImage(baseImage);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;

                using var roundedImage = profilePicture.Frames[0].RoundImage();
                graphics.DrawImage(roundedImage, 70, 70, 160, 160);

                var tmpFile = new TemporaryFile("png");
                baseImage.Save(tmpFile.Path, SysDraw.Imaging.ImageFormat.Png);

                return tmpFile;
            }
        }

        private SysDraw.Image RenderPointsBaseFrame(User userEntity, int position, IUser user)
        {
            var bitmap = new Bitmap(1000, 300);
            bitmap.MakeTransparent();

            using var graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;

            graphics.RenderRectangle(new Rectangle(0, 0, bitmap.Width, bitmap.Height), SysDraw.Color.FromArgb(35, 39, 42), 15);
            graphics.RenderRectangle(new Rectangle(50, 50, 900, 200), SysDraw.Color.FromArgb(100, 0, 0, 0), 15);

            var positionTextSize = graphics.MeasureString($"#{position}", PositionFont);
            graphics.DrawString("BODY", TitleTextFont, WhiteBrush, new PointF(250, 180));
            graphics.DrawString(userEntity.Points.ToString(), PositionFont, LightGrayBrush, new PointF(340, 150));
            var positionTitleTextSize = graphics.MeasureString("POZICE", TitleTextFont);
            graphics.DrawString("POZICE", TitleTextFont, WhiteBrush, new PointF(900 - positionTextSize.Width - positionTitleTextSize.Width, 180));
            graphics.DrawString($"#{position}", PositionFont, WhiteBrush, new PointF(910 - positionTextSize.Width, 150));

            var nickname = user.GetDisplayName();
            graphics.MeasureAndShrinkText(ref nickname, NicknameFont, 725, appendEllipsis: true);
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
