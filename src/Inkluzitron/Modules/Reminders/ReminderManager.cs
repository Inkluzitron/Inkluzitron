using Discord;
using Inkluzitron.Data;
using Inkluzitron.Data.Entities;
using Inkluzitron.Models.Reminders;
using Inkluzitron.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inkluzitron.Modules.Reminders
{
    internal class ReminderManager
    {
        public const string ScheduledTaskDiscriminator = "Reminder";

        private DatabaseFactory DatabaseFactory { get; }
        private ScheduledTasksService ScheduledTasksService { get; }

        public ReminderManager(DatabaseFactory databaseFactory, ScheduledTasksService scheduledTasksService)
        {
            DatabaseFactory = databaseFactory;
            ScheduledTasksService = scheduledTasksService;
        }

        public async Task<Reminder> FindAsync(long id)
        {
            using var dbContext = DatabaseFactory.Create();
            return await dbContext.Reminders.FindAsync(id);
        }

        public async Task<ReminderEmbedPage> FindForUserAsync(
            ulong userId,
            (long, DateTimeOffset)? findAfter = null,
            (long, DateTimeOffset)? findBefore = null,
            bool findLast = false,
            bool findFirst = false
        )
        {
            var specifiedArguments = (findAfter == null ? 0 : 1)
                                     + (findBefore == null ? 0 : 1)
                                     + (findLast ? 0 : 1)
                                     + (findFirst ? 0 : 1);
            if (specifiedArguments > 1)
                throw new ArgumentException("Only one filter argument can be specified.");

            using var dbContext = DatabaseFactory.Create();

            var remindersForUser = dbContext.Reminders
                .AsAsyncEnumerable()
                .Where(r => r.OwnerId == userId);

            var pageCount = await remindersForUser.CountAsync();
            var pageNumber = Math.Min(1, pageCount);
            Reminder reminder = null;

            remindersForUser = remindersForUser.OrderBy(r => r.When).ThenBy(r => r.ReminderId);

            if (findFirst)
                reminder = await remindersForUser.FirstOrDefaultAsync();
            else if (findLast)
                reminder = await remindersForUser.LastOrDefaultAsync();
            else if (findAfter is (long afterReminderId, DateTimeOffset afterDateTime))
            {
                var nextReminders = remindersForUser.Where(r => r.When > afterDateTime || (r.When == afterDateTime && r.ReminderId > afterReminderId));
                pageNumber = pageCount - await nextReminders.CountAsync();
                reminder = await nextReminders.FirstOrDefaultAsync()
                           ?? await remindersForUser.LastOrDefaultAsync();
            }
            else if (findBefore is (long beforeReminderId, DateTimeOffset beforeDateTime))
            {
                var previousReminders = remindersForUser.Where(r => r.When < beforeDateTime || (r.When == beforeDateTime && r.ReminderId < beforeReminderId));
                pageNumber = await remindersForUser.CountAsync();
                reminder = await previousReminders.LastOrDefaultAsync()
                    ?? await remindersForUser.FirstOrDefaultAsync();
            }

            return new ReminderEmbedPage
            {
                PageNumber = pageNumber,
                PageCount = pageCount,
                Reminder = reminder
            };
        }

        public async Task<Reminder> CreateAsync(IUserMessage message, string reason, DateTimeOffset when)
        {
            using var dbContext = DatabaseFactory.Create();

            var reminder = new Reminder
            {
                MessageUrl = message.GetJumpUrl(),
                Reason = reason,
                OwnerId = message.Author.Id,
                When = when
            };

            dbContext.Reminders.Add(reminder);
            dbContext.ReminderSubscriptions.Add(new ReminderSubscription
            {
                Reminder = reminder,
                UserId = message.Author.Id
            });

            await dbContext.SaveChangesAsync();

            await ScheduledTasksService.EnqueueAsync(new ScheduledTask
            {
                Discriminator = ScheduledTaskDiscriminator,
                Tag = reminder.ReminderId.ToString(),
                When = when
            });

            return reminder;
        }

        public async Task CancelAsync(Reminder reminder)
        {
            using var dbContext = DatabaseFactory.Create();
            dbContext.Reminders.Remove(reminder);
            await dbContext.SaveChangesAsync();
            await ScheduledTasksService.CancelAsync(ScheduledTaskDiscriminator, reminder.ReminderId.ToString());
        }
    }
}
