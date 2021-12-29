using Discord;
using Discord.WebSocket;
using Inkluzitron.Contracts;
using Inkluzitron.Data;
using Inkluzitron.Data.Entities;
using Inkluzitron.Enums;
using Inkluzitron.Models.Settings;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Inkluzitron.Services
{
    public class BirthdayNotificationService : IScheduledTaskHandler
    {
        private const string ScheduledTaskDiscriminator = "BirthdayNotification";
        private const string ScheduledTaskTag = null;

        private BirthdaySettings Settings { get; }
        private DatabaseFactory DbFactory { get; }
        private ScheduledTasksService ScheduledTasksService { get; }
        private UsersService UsersService { get; }
        private DiscordSocketClient Client { get; }
        private BotSettings BotSettings { get; }
        private ILogger<BirthdayNotificationService> Logger { get; }

        public BirthdayNotificationService(
            BirthdaySettings settings, DatabaseFactory dbFactory, ScheduledTasksService scheduledTasksService,
            UsersService usersService, DiscordSocketClient client, BotSettings botSettings, ILogger<BirthdayNotificationService> logger
        )
        {
            Settings = settings;
            DbFactory = dbFactory;
            ScheduledTasksService = scheduledTasksService;
            UsersService = usersService;
            Client = client;
            BotSettings = botSettings;
            Logger = logger;
        }

        static private bool HasBirthdayOnTwentyNinthOfFebruary(User user)
            => user.BirthdayDate != null
            && user.BirthdayDate.Value.Month == 2
            && user.BirthdayDate.Value.Day == 29;

        public async Task<Embed> ComposeBirthdaysEmbedAsync(SocketGuild guild, bool returnNullWhenNoBirthdays)
        {
            using var dbContext = DbFactory.Create();
            var today = DateTime.Now;

            bool HasBirthdayToday(User user)
                => user.BirthdayDate != null
                && user.BirthdayDate.Value.Month == today.Month
                && user.BirthdayDate.Value.Day == today.Day;

            var birthdayUsers = dbContext.Users.AsQueryable()
                .Where(HasBirthdayToday);

            if (!DateTime.IsLeapYear(today.Year) && today.Month == 3 && today.Day == 1)
            {
                birthdayUsers = birthdayUsers.Union(
                    dbContext.Users
                    .AsQueryable()
                    .Where(HasBirthdayOnTwentyNinthOfFebruary)
                );
            }

            var embed = new EmbedBuilder()
                .WithColor(Color.Green)
                .WithTitle(Settings.BirthdayListHeader);

            await guild.DownloadUsersAsync();

            var sortedBirthdayEntries = birthdayUsers.ToAsyncEnumerable()
                .SelectAwait(async dbUser => (
                    Age: dbUser.BirthdayDate.Value.Year is int yearOfBirth && yearOfBirth != User.UnsetBirthdayYear
                         ? today.Year - yearOfBirth
                         : int.MaxValue,
                    GuildUser: guild.GetUser(dbUser.Id),
                    DisplayName: await UsersService.GetDisplayNameAsync(dbUser.Id)
                ))
                .Where(u => u.GuildUser != null)
                .OrderBy(u => u.Age)
                .ThenBy(u => u.DisplayName, StringComparer.CurrentCultureIgnoreCase);

            await foreach (var birthdayEntry in sortedBirthdayEntries)
            {
                var isAgeAvailable = birthdayEntry.Age != int.MaxValue;
                var displayName = birthdayEntry.DisplayName;
                var username = birthdayEntry.GuildUser.Username;
                var discriminator = birthdayEntry.GuildUser.Discriminator;
                var displayNameWithAge = isAgeAvailable
                    ? $"{displayName} ({birthdayEntry.Age} {Settings.YearsOld})"
                    : displayName;

                embed.AddField(
                    Format.Sanitize(displayNameWithAge),
                    Format.Sanitize(username + "#" + discriminator)
                );
            }

            if (embed.Fields.Count > 0)
                embed.WithFooter(Settings.BirthdayListFooter); // add wishes
            else if (returnNullWhenNoBirthdays)
                return null;
            else
                embed.Description = Settings.NoBirthdaysTodayMessage;

            return embed.Build();
        }

        async Task IScheduledTaskHandler.InitializeAsync()
        {
            var scheduledTasks = await ScheduledTasksService.LookupAsync(ScheduledTaskDiscriminator, ScheduledTaskTag);
            var expectedHours = Settings.BirthdayNotificationTime.Hours;
            var expectedMinutes = Settings.BirthdayNotificationTime.Minutes;

            var allOk = scheduledTasks.Count == 1
                && scheduledTasks.All(
                    t => t.When.ToLocalTime().Hour == expectedHours
                    && t.When.ToLocalTime().Minute == expectedMinutes
            );

            if (allOk)
                return; // The scheduled task exists and it's set to the correct hours/minutes according to config.

            foreach (var scheduledTask in scheduledTasks)
                await ScheduledTasksService.CancelAsync(scheduledTask.ScheduledTaskId);

            var now = DateTimeOffset.Now;

            await ScheduledTasksService.EnqueueAsync(new ScheduledTask
            {
                Discriminator = ScheduledTaskDiscriminator,
                Tag = ScheduledTaskTag,
                When = new DateTimeOffset(now.Year, now.Month, now.Day, expectedHours, expectedMinutes, 0, now.Offset)
            });
        }

        async Task<ScheduledTaskResult> IScheduledTaskHandler.HandleAsync(ScheduledTask scheduledTask)
        {
            if (scheduledTask.Discriminator != ScheduledTaskDiscriminator)
                return ScheduledTaskResult.NotHandled;
            if (scheduledTask.Tag != ScheduledTaskTag)
                return ScheduledTaskResult.NotHandled;

            try
            {
                var homeGuild = Client.GetGuild(BotSettings.HomeGuildId);
                if (homeGuild is null)
                    throw new InvalidOperationException("Home guild not found.");

                var birthdayNotificationChannel = homeGuild.GetTextChannel(Settings.BirthdayNotificationChannelId);
                if (birthdayNotificationChannel is null)
                    throw new InvalidOperationException("Birthday channel not found.");

                var birthdaysEmbed = await ComposeBirthdaysEmbedAsync(homeGuild, returnNullWhenNoBirthdays: true);
                if (birthdaysEmbed != null)
                    await birthdayNotificationChannel.SendMessageAsync(embed: birthdaysEmbed);

                // Repeat again tomorrow
                var tomorrow = DateTimeOffset.Now.AddDays(1);
                scheduledTask.When = new DateTimeOffset(
                    tomorrow.Year, tomorrow.Month, tomorrow.Day,
                    Settings.BirthdayNotificationTime.Hours, Settings.BirthdayNotificationTime.Minutes, 0,
                    tomorrow.Offset
                );
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Could not process today's birthdays. Will try again in 1 hour.");
                scheduledTask.When = DateTimeOffset.Now.AddHours(1);
            }

            return ScheduledTaskResult.HandledAndPostponed;
        }
    }
}
