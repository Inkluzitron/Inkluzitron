using Discord;
using Discord.WebSocket;
using ImageMagick;
using Inkluzitron.Data;
using Inkluzitron.Data.Entities;
using Inkluzitron.Extensions;
using Inkluzitron.Models;
using Inkluzitron.Models.Settings;
using Inkluzitron.Utilities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Inkluzitron.Services
{
    public class PointsService
    {
        static private readonly DateTime FallbackDateTime = new(2000, 1, 1);
        static private readonly TimeSpan MessageIncrementCooldown = TimeSpan.FromSeconds(60);
        static private readonly TimeSpan ReactionIncrementCooldown = TimeSpan.FromSeconds(30);
        static private readonly NumberFormatInfo NumberFormat = new CultureInfo("cs-CZ").NumberFormat;

        private DatabaseFactory DatabaseFactory { get; }
        private DiscordSocketClient DiscordClient { get; }
        private ImagesService ImagesService { get; }

        private DrawableFont DataFont { get; }
        private DrawableFontPointSize DataSize { get; }
        private DrawableFont NicknameFont { get; }
        private DrawableFontPointSize NicknameSize { get; }
        private DrawableFont LabelFont { get; }
        private DrawableFontPointSize LabelSize { get; }
        private BotSettings BotSettings { get; }
        private UsersService UsersService { get; }

        public PointsService(DatabaseFactory factory, DiscordSocketClient discordClient,
            ImagesService imagesService, BotSettings botSettings, UsersService usersService)
        {
            DatabaseFactory = factory;
            DiscordClient = discordClient;
            ImagesService = imagesService;
            BotSettings = botSettings;

            DiscordClient.ReactionAdded += OnReactionAddedAsync;
            DiscordClient.ReactionRemoved += OnReactionRemovedAsync;

            const string font = "Comic Sans MS";
            DataFont = new DrawableFont(font);
            DataSize = new DrawableFontPointSize(55);
            NicknameFont = new DrawableFont(font);
            NicknameSize = new DrawableFontPointSize(40);
            LabelFont = new DrawableFont(font);
            LabelSize = new DrawableFontPointSize(24);
            UsersService = usersService;
        }

        private async Task<IUser> LookupUserAsync(ulong userId)
        {
            var guild = DiscordClient.GetGuild(BotSettings.HomeGuildId);

            if (guild != null && await guild.GetUserAsync(userId) is SocketGuildUser guildUser)
                return guildUser;
            else if (await DiscordClient.Rest.GetUserAsync(userId) is IUser restUser)
                return restUser;
            else
                return null;
        }

        public async Task<IReadOnlyList<GraphItem>> GetAllPointsAsync()
        {
            using var context = DatabaseFactory.Create();
            var result = new List<GraphItem>();

            await foreach (var userEntry in context.Users.AsQueryable().OrderBy(u => u.Points).AsAsyncEnumerable())
            {
                if (await LookupUserAsync(userEntry.Id) is not IUser user)
                    continue;

                result.Add(new GraphItem
                {
                    UserId = user.Id,
                    UserDisplayName = await UsersService.GetDisplayNameAsync(user),
                    Value = userEntry.Points
                });
            }

            return result;
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

            var msg = await message.GetOrDownloadAsync();
            if (msg == null || msg.Author == user) return;
            await AddPointsAsync(msg.Author, BotSettings.PointsKarmaIncrement);
        }

        private async Task OnReactionRemovedAsync(Cacheable<IUserMessage, ulong> message, ISocketMessageChannel channel, SocketReaction reaction)
        {
            if (channel is not IGuildChannel) return;

            var user = reaction.User.IsSpecified ? reaction.User.Value : await DiscordClient.Rest.GetUserAsync(reaction.UserId);

            var msg = await message.GetOrDownloadAsync();
            if (msg == null || msg.Author == user) return;

            await AddPointsAsync(msg.Author, -BotSettings.PointsKarmaIncrement);
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
                var userEntity = await context.GetOrCreateUserEntityAsync(user);
                if (isOnCooldownFunc(userEntity))
                    return;

                userEntity.Points += amount;
                resetCooldownAction(userEntity);
                await context.SaveChangesAsync();
            });
        }

        public async Task AddPointsAsync(IUser user, int points)
        {
            using var context = DatabaseFactory.Create();

            await Patiently.HandleDbConcurrency(async () =>
            {
                var userEntity = await context.GetOrCreateUserEntityAsync(user);
                userEntity.Points += points;
                await context.SaveChangesAsync();
            });
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
                userEntity = await context.GetOrCreateUserEntityAsync(user);
                position = await GetUserPositionAsync(context, user);
            }

            using var profilePicture = await ImagesService.GetAvatarAsync(user);

            if (profilePicture.IsAnimated)
            {
                var tmpFile = new TemporaryFile("gif");
                using var output = new MagickImageCollection();

                using var baseImage = await RenderPointsBaseFrame(userEntity, position, user);
                foreach (var frameAvatar in profilePicture.Frames)
                {
                    frameAvatar.Resize(160, 160);
                    frameAvatar.RoundImage();

                    var frame = baseImage.Clone();
                    frame.Composite(frameAvatar, 70, 70, CompositeOperator.Over);
                    frame.AnimationDelay = frameAvatar.AnimationDelay;
                    frame.GifDisposeMethod = GifDisposeMethod.Background;
                    output.Add(frame);
                }

                output.Coalesce();
                output.Write(tmpFile.Path, MagickFormat.Gif);
                return tmpFile;
            }
            else
            {
                using var baseImage = await RenderPointsBaseFrame(userEntity, position, user);

                var avatar = profilePicture.Frames[0];
                avatar.Resize(160, 160);
                avatar.RoundImage();
                baseImage.Composite(avatar, 70, 70, CompositeOperator.Over);

                var tmpFile = new TemporaryFile("png");
                baseImage.Write(tmpFile.Path, MagickFormat.Png);

                return tmpFile;
            }
        }

        private async Task<MagickImage> RenderPointsBaseFrame(User userEntity, int position, IUser user)
        {
            var image = new MagickImage(MagickColors.Transparent, 1000, 300);

            var positionText = $"#{position}";
            var nickname = await UsersService.GetDisplayNameAsync(user);

            var drawable = new Drawables()
                .FillColor(MagickColor.FromRgba(35, 39, 42, 255))
                .RoundRectangle(0, 0, image.Width, image.Height, 15, 15)
                .FillColor(MagickColor.FromRgba(0, 0, 0, 100))
                .RoundRectangle(50, 50, image.Width - 50, image.Height - 50, 15, 15)
                .FillColor(MagickColors.LightGray)
                .Font(DataFont)
                .FontPointSize(DataSize)
                .Text(330, 218, userEntity.Points.ToString("N0", NumberFormat))
                .TextAlignment(TextAlignment.Right)
                .Text(920, 218, positionText);

            var positionTextMetrics = drawable.FontTypeMetrics(positionText);

            drawable
                .Font(LabelFont)
                .FontPointSize(LabelSize)
                .FillColor(MagickColors.White)
                .Text(900 - positionTextMetrics.TextWidth, 210, "POZICE")
                .TextAlignment(TextAlignment.Left)
                .Text(250, 210, "BODY")
                .Draw(image);

            image.DrawEnhancedText(nickname, 250, 70, MagickColors.White, NicknameFont, NicknameSize.PointSize, 670);

            return image;
        }
    }
}
