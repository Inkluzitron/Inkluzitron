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

        public async Task<Embed> ComposeBirthdaysEmbedAsync(SocketGuild guild, bool returnNullWhenNoBirthdays)
        {
            using var dbContext = DbFactory.Create();
            var today = DateTime.Now;

            var birthdayUsers = dbContext.Users.AsQueryable()
                .Where(u => u.BirthdayDate != null
                            && u.BirthdayDate.Value.Month == today.Month
                            && u.BirthdayDate.Value.Day == today.Day
                )
                .ToAsyncEnumerable();

            var embed = new EmbedBuilder()
                .WithColor(Color.Green)
                .WithTitle(Settings.BirthdayListHeader);

            await guild.DownloadUsersAsync();

            await foreach (var birthdayUser in birthdayUsers)
            {
                var guildUser = guild.GetUser(birthdayUser.Id);
                if (guildUser == null)
                    continue;

                var isAgeAvailable = birthdayUser.BirthdayDate.Value.Year != User.UnsetBirthdayYear;
                var displayName = await UsersService.GetDisplayNameAsync(guildUser.Id);
                var username = guildUser.Username;
                var discriminator = guildUser.Discriminator;
                var displayNameWithAge = isAgeAvailable
                    ? $"{displayName} ({today.Year - birthdayUser.BirthdayDate.Value.Year} {Settings.YearsOld})"
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
                When = new DateTimeOffset(now.Year, now.Month, now.Day, expectedMinutes, expectedHours, 0, now.Offset)
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
                var now = DateTimeOffset.Now;
                scheduledTask.When = new DateTimeOffset(
                    now.Year, now.Month, now.Day + 1,
                    Settings.BirthdayNotificationTime.Hours, Settings.BirthdayNotificationTime.Minutes, 0,
                    now.Offset
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
