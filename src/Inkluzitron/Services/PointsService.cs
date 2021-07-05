using Discord;
using Discord.WebSocket;
using ImageMagick;
using Inkluzitron.Contracts;
using Inkluzitron.Data;
using Inkluzitron.Data.Entities;
using Inkluzitron.Enums;
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
    public class PointsService : IReactionHandler
    {
        static private readonly DateTime FallbackDateTime = new(2000, 1, 1);
        static private readonly TimeSpan MessageIncrementCooldown = TimeSpan.FromSeconds(60);
        static private readonly TimeSpan ReactionIncrementCooldown = TimeSpan.FromSeconds(30);
        static private readonly NumberFormatInfo NumberFormat = new CultureInfo("cs-CZ").NumberFormat;

        private DatabaseFactory DatabaseFactory { get; }
        private DiscordSocketClient DiscordClient { get; }
        private ImagesService ImagesService { get; }
        private BotSettings BotSettings { get; }
        private UsersService UsersService { get; }
        private KisService KisService { get; }

        private DrawableFont DataFont { get; }
        private double DataFontSize { get; }
        private DrawableFont NicknameFont { get; }
        private double NicknameFontSize { get; }
        private DrawableFont LabelFont { get; }
        private double LabelFontSize { get; }
        private DrawableFont SmallLabelFont { get; }
        private double SmallLabelFontSize { get; }
        private DrawableFont SmallDataFont { get; }
        private double SmallDataFontSize { get; }

        public PointsService(DatabaseFactory factory, DiscordSocketClient discordClient,
            ImagesService imagesService, BotSettings botSettings, UsersService usersService,
            KisService kisService)
        {
            DatabaseFactory = factory;
            DiscordClient = discordClient;
            ImagesService = imagesService;
            BotSettings = botSettings;
            KisService = kisService;
            UsersService = usersService;

            const string font = "Open Sans";
            DataFont = new DrawableFont(font) { Weight = FontWeight.Bold };
            DataFontSize = 40;
            NicknameFont = new DrawableFont(font) { Weight = FontWeight.Bold };
            NicknameFontSize = 40;
            LabelFont = new DrawableFont(font);
            LabelFontSize = 20;
            SmallLabelFont = new DrawableFont(font);
            SmallLabelFontSize = 15;
            SmallDataFont = new DrawableFont(font) { Weight = FontWeight.SemiBold };
            SmallDataFontSize = 24;
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

        public async Task<IReadOnlyList<GraphItem>> GetUsersTotalPointsAsync()
        {
            using var context = DatabaseFactory.Create();
            var result = new List<GraphItem>();

            var usersPoints = context.Users.Include(u => u.DailyActivity).AsQueryable()
                .Select(u => new
                {
                    u.Id,
                    TotalPoints = u.DailyActivity.Sum(p => p.Points)
                })
                .OrderBy(u => u.TotalPoints)
                .AsAsyncEnumerable();

            await foreach (var userEntry in usersPoints)
            {
                if (await LookupUserAsync(userEntry.Id) is not IUser user)
                    continue;

                result.Add(new GraphItem
                {
                    UserId = user.Id,
                    UserDisplayName = await UsersService.GetDisplayNameAsync(user),
                    Value = userEntry.TotalPoints
                });
            }

            return result;
        }

        async Task<bool> IReactionHandler.HandleReactionAddedAsync(IUserMessage message, IEmote reaction, IUser user)
        {
            if (message.Channel is not IGuildChannel)
                return false; // Only server messages increments points.

            if (user.IsBot)
                return false;

            await AddIncrementalPointsAsync(
                user,
                ThreadSafeRandom.Next(0, 10),
                u => DateTime.UtcNow.Subtract(u.LastReactionPointsIncrement ?? FallbackDateTime) < ReactionIncrementCooldown,
                u => u.LastReactionPointsIncrement = DateTime.UtcNow,
                UserActivityType.ReactionAdded
            );

            if (message == null || message.Author == user || message.Author.IsBot)
                return false;

            await AddPointsAsync(message.Author, BotSettings.PointsKarmaIncrement);
            return false;
        }

        async Task<bool> IReactionHandler.HandleReactionRemovedAsync(IUserMessage message, IEmote _, IUser user)
        {
            if (message.Channel is not IGuildChannel)
                return false;

            if (message == null || message.Author == user || message.Author.IsBot)
                return false;

            await AddPointsAsync(message.Author, -BotSettings.PointsKarmaIncrement);
            return false;
        }

        public async Task IncrementAsync(SocketMessage message)
        {
            if (message.Author.IsBot)
                return;

            await AddIncrementalPointsAsync(
                message.Author,
                ThreadSafeRandom.Next(0, 25),
                u => DateTime.UtcNow.Subtract(u.LastMessagePointsIncrement ?? FallbackDateTime) < MessageIncrementCooldown,
                u => u.LastMessagePointsIncrement = DateTime.UtcNow,
                UserActivityType.MessageSent
            );
        }

        public async Task AddIncrementalPointsAsync(IUser user, int amount, Func<User, bool> isOnCooldownFunc,
            Action<User> resetCooldownAction, UserActivityType activityType = UserActivityType.Other)
        {
            if (user.IsBot)
                return;

            using var context = DatabaseFactory.Create();

            await Patiently.HandleDbConcurrency(async () =>
            {
                var userEntity = await context.GetOrCreateUserEntityAsync(user);
                if (isOnCooldownFunc(userEntity))
                    return;

                var activity = await context.GetTodayUserActivityAsync(user);
                activity.Points += amount;

                if (activityType == UserActivityType.MessageSent)
                    activity.MessagesSent++;
                else if (activityType == UserActivityType.ReactionAdded)
                    activity.ReactionsAdded++;

                resetCooldownAction(userEntity);
                await context.SaveChangesAsync();
            });
        }

        public async Task AddPointsAsync(IUser user, int points)
        {
            if (user.IsBot)
                return;

            using var context = DatabaseFactory.Create();
            await AddPointsAsync(context, user, points);
        }

        static public async Task AddPointsAsync(BotDatabaseContext context, IUser user, int points)
        {
            if (user.IsBot)
                return;

            await Patiently.HandleDbConcurrency(async () =>
            {
                var activity = await context.GetTodayUserActivityAsync(user);
                activity.Points += points;
                await context.SaveChangesAsync();
            });
        }

        public async Task<int> GetUserPositionAsync(IUser user, DateTime? from = null)
        {
            using var context = DatabaseFactory.Create();
            return await GetUserPositionAsync(context, user, from);
        }

        static private async Task<int> GetUserPositionAsync(BotDatabaseContext context, IUser user, DateTime? from = null)
        {
            if (user.IsBot)
                return 0;

            var index = await context.Users.AsQueryable()
                .Where(u => u.Id == user.Id)
                .Select(u => u.DailyActivity.Where(p => !from.HasValue || p.Day >= from.Value).Sum(p => p.Points))
                .Select(ownPoints => context.Users.AsQueryable()
                    .Where(u => u.DailyActivity.Where(p => !from.HasValue || p.Day >= from.Value).Sum(p => p.Points) > ownPoints)
                    .Count())
                .FirstAsync();

            return index + 1;
        }

        public async Task<long> GetTotalPointsAsync(IUser user, DateTime? from = null)
        {
            using var context = DatabaseFactory.Create();
            return await context.DailyUsersActivities.AsQueryable()
                .Where(a =>
                    a.UserId == user.Id &&
                    (!from.HasValue || a.Day >= from.Value))
                .SumAsync(a => a.Points);
        }

        public async Task<List<PointsLeaderboardData>> GetLeaderboardAsync(int startFrom = 0, int count = 10, DateTime? from = null)
        {
            using var context = DatabaseFactory.Create();
            var userPoints = context.Users.Include(u => u.DailyActivity).AsQueryable()
                .Select(u => new { u.Id, Points = u.DailyActivity.Where(p => !from.HasValue || p.Day >= from.Value).Sum(p => p.Points) })
                .OrderByDescending(u => u.Points)
                .Skip(startFrom).Take(count)
                .AsAsyncEnumerable();

            var board = new List<PointsLeaderboardData>();

            await foreach (var user in userPoints)
            {
                startFrom++;
                board.Add(new PointsLeaderboardData()
                {
                    Position = startFrom,
                    UserDisplayName = await UsersService.GetDisplayNameAsync(user.Id),
                    Points = user.Points
                });
            }

            return board;
        }

        public async Task<int> GetUserCountAsync()
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

            var ppDominantColor = profilePicture.Frames[0].GetDominantColor();

            if (profilePicture.IsAnimated)
            {
                var tmpFile = new TemporaryFile("gif");
                using var output = new MagickImageCollection();

                using var baseImage = await RenderPointsBaseFrameAsync(position, user, ppDominantColor);
                foreach (var frameAvatar in profilePicture.Frames)
                {
                    frameAvatar.Resize(166, 166);
                    frameAvatar.RoundImage();

                    var frame = baseImage.Clone();
                    frame.Composite(frameAvatar, 57, 57, CompositeOperator.Over);
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
                using var baseImage = await RenderPointsBaseFrameAsync(position, user, ppDominantColor);

                var avatar = profilePicture.Frames[0];
                avatar.Resize(166, 166);
                avatar.RoundImage();
                baseImage.Composite(avatar, 57, 57, CompositeOperator.Over);

                var tmpFile = new TemporaryFile("png");
                baseImage.Write(tmpFile.Path, MagickFormat.Png);

                return tmpFile;
            }
        }

        private async Task DrawGraphOnPointsImage(MagickImage image, IUser user, int width, int height)
        {
            var days = BotSettings.UserPointsGraphDays;

            var graphPoints = new List<PointD>();
            var graphData = new List<DailyUserActivity>();
            var today = DateTime.Today;
            var day = today.AddDays(-days);

            using var context = DatabaseFactory.Create();
            var dailyPoints = await context.DailyUsersActivities.AsQueryable()
                .Where(a => a.UserId == user.Id && a.Day >= day)
                .ToListAsync();

            for (; day < today; day = day.AddDays(1))
            {
                var dayData = dailyPoints.FirstOrDefault(p => p.Day == day)
                    ?? new DailyUserActivity() { Day = day, Points = 0 };

                graphData.Add(dayData);
            }

            var min = graphData.Min(p => p.Points);
            var max = graphData.Max(p => p.Points);

            if (min == max)
                return;

            foreach (var data in graphData)
            {
                var dateDiff = today - data.Day;

                graphPoints.Add(new PointD(
                    width - (double)dateDiff.Days / days * width,
                    height - (double)(data.Points - min) / (max - min) * height));
            }

            new Drawables()
                .Density(100)
                .Translation(620, 220)
                .StrokeWidth(2)
                .StrokeColor(MagickColor.FromRgb(50, 50, 50))
                .FillColor(MagickColors.Transparent)
                .Line(0, height, width, height)
                .Line(0, 0, width, 0)
                .StrokeColor(MagickColors.Gray)
                .StrokeWidth(3)
                .Lines(graphPoints.ToArray())
                .StrokeColor(MagickColors.Transparent)
                .FillColor(MagickColor.FromRgb(50, 50, 50))
                .Font(SmallLabelFont)
                .FontPointSize(SmallLabelFontSize)
                .TextAlignment(TextAlignment.Right)
                .Text(-5, 7, $"{max}") // TODO in the future maybe pretty format the axis labels
                .Text(-5, 7 + height, $"{min}")
                .Draw(image);
        }

        private async Task<MagickImage> RenderPointsBaseFrameAsync(int position, IUser user, IMagickColor<byte> headerColor)
        {
            var pastWeek = DateTime.Today.AddDays(-6);
            var pastMonth = DateTime.Today.AddMonths(-1).AddDays(1);

            using var image = new MagickImage(MagickColors.Transparent, 900, 320);

            var positionText = $"#{position}";
            var nickname = await UsersService.GetDisplayNameAsync(user);

            var headerHsl = ColorHSL.FromMagickColor(headerColor);
            var nicknameColor = headerHsl.Lightness > 0.75 ? MagickColors.Black : MagickColors.White;

            var drawable = new Drawables()
                .TextAlignment(TextAlignment.Left)
                .Density(100)
                .FillColor(headerColor)
                .RoundRectangle(0, 0, image.Width, 140, 12, 12)
                .FillColor(MagickColor.FromRgb(24, 25, 28))
                .RoundRectangle(0, 120, image.Width, image.Height, 12, 12)
                .Rectangle(0, 120, image.Width, 140)
                .Circle(120, 120, 120, 25)
                .FillColor(MagickColors.LightGray)
                .Font(DataFont)
                .FontPointSize(DataFontSize)
                .Text(330, 190, (await GetTotalPointsAsync(user)).ToString("N0", NumberFormat))
                .TextAlignment(TextAlignment.Right)
                .Text(870, 190, positionText);

            var positionTextMetrics = drawable.FontTypeMetrics(positionText);

            drawable = drawable
                .Font(LabelFont)
                .FontPointSize(LabelFontSize)
                .FillColor(MagickColors.WhiteSmoke)
                .Text(860 - positionTextMetrics.TextWidth, 182, "POZICE")
                .Text(318, 182, "BODY")
                .Font(SmallLabelFont)
                .FontPointSize(SmallLabelFontSize)
                .TextAlignment(TextAlignment.Right)
                .FillColor(MagickColors.LightGray)
                .Text(318, 240, "TÝDEN")
                .Text(318, 280, "MĚSÍC")
                .Font(SmallDataFont)
                .FontPointSize(SmallDataFontSize)
                .FillColor(MagickColors.DarkGray);

            var weekPoints = (await GetTotalPointsAsync(user, pastWeek)).ToString("N0", NumberFormat);
            var monthPoints = (await GetTotalPointsAsync(user, pastMonth)).ToString("N0", NumberFormat);
            var smallPositionX = 330 + Math.Max(
                drawable.FontTypeMetrics(weekPoints).TextWidth,
                drawable.FontTypeMetrics(monthPoints).TextWidth);

            drawable
                .Text(smallPositionX, 243, weekPoints)
                .Text(smallPositionX, 283, monthPoints)
                .TextAlignment(TextAlignment.Left)
                .Text(25 + smallPositionX, 243, $"#{await GetUserPositionAsync(user, pastWeek)}")
                .Text(25 + smallPositionX, 283, $"#{await GetUserPositionAsync(user, pastMonth)}")
                .Draw(image);

            image.DrawEnhancedText(nickname, 240, 45, nicknameColor, NicknameFont, NicknameFontSize, 635);

            await DrawGraphOnPointsImage(image, user, 250, 60);

            var finalImage = new MagickImage(MagickColors.Transparent, image.Width + 40, image.Height + 40);

            new Drawables()
                .FillColor(MagickColor.FromRgba(0, 0, 0, 50))
                .RoundRectangle(20, 20, finalImage.Width - 20, finalImage.Height - 20, 12, 12)
                .Draw(finalImage);

            finalImage.Blur(0, 10);
            finalImage.Composite(image, 20, 20, CompositeOperator.Over);

            return finalImage;
        }

        public async Task<string> SynchronizeKisPointsAsync(IUser user)
        {
            using var context = DatabaseFactory.Create();

            var now = DateTime.UtcNow;
            var userEntity = await context.GetOrCreateUserEntityAsync(user);

            if (userEntity.KisLastCheck != null && userEntity.KisLastCheck.Value.AddDays(KisService.Settings.SyncDays) > DateTime.UtcNow)
            {
                return string.Format(KisService.Settings.Messages["SyncTooSoon"],
                    KisService.Settings.SyncDays,
                    new FormatByValue(KisService.Settings.SyncDays),
                    userEntity.KisLastCheck.Value.AddDays(KisService.Settings.SyncDays + 1));
            }

            var points = await KisService.GetPrestigeAsync(userEntity.KisNickname, userEntity.KisLastCheck, now);

            if (!points.IsOk)
                return points.ErrorMessage;

            var calculatedPoints = points.Prestige * KisService.Settings.PointsMultiplication;
            await AddPointsAsync(context, user, calculatedPoints);
            await Patiently.HandleDbConcurrency(async () =>
            {
                var entity = await context.GetOrCreateUserEntityAsync(user);

                entity.KisLastCheck = now;
                await context.SaveChangesAsync();
            });

            return string.Format(KisService.Settings.Messages["Done"],
                calculatedPoints,
                new FormatByValue(calculatedPoints));
        }
    }
}
