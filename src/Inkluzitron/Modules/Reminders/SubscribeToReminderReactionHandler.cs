using Discord;
using Inkluzitron.Contracts;
using Inkluzitron.Data.Entities;
using Inkluzitron.Models.Settings;
using Inkluzitron.Services;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;

namespace Inkluzitron.Modules.Reminders
{
    // TODO: ReminderDefinitionParser etc.

    public class SubscribeToReminderReactionHandler : IReactionHandler
    {
        public ReactionSettings ReactionSettings { get; }
        public ScheduledTasksService ScheduledTasksService { get; }

        public SubscribeToReminderReactionHandler(ReactionSettings reactionSettings, ScheduledTasksService scheduledTasksService)
        {
            ReactionSettings = reactionSettings;
            ScheduledTasksService = scheduledTasksService;
        }

        public async Task<bool> HandleReactionAddedAsync(IUserMessage message, IEmote reaction, IUser user)
        {
            if (user.IsBot)
                return false;
            if (!ReactionSettings.RemindMeToo.Equals(reaction))
                return false;

            var reminders = await ScheduledTasksService.LookupAsync(
                RemindersModule.Discriminator,
                t => t.Data.Contains($@"""messageUrl"":""{message.GetJumpUrl()}"",")
            );

            var reminder = reminders.FirstOrDefault();
            if (reminder == null)
                return false;

            if (reminders.Any(r => r.ParseData<ReminderData>().UserId == user.Id))
                return false;

            var reminderData = reminder.ParseData<ReminderData>();
            var clone = new ScheduledTask
            {
                Discriminator = reminder.Discriminator,
                Tag = reminder.Tag,
                When = reminder.When,
                Data = JsonConvert.SerializeObject(new ReminderData
                {
                    MessageUrl = reminderData.MessageUrl,
                    Reason = reminderData.Reason,
                    UserId = user.Id
                })
            };

            await ScheduledTasksService.EnqueueAsync(clone);
            return true;
        }

        public async Task<bool> HandleReactionRemovedAsync(IUserMessage message, IEmote reaction, IUser user)
        {
            if (user.IsBot)
                return false;
            if (!ReactionSettings.RemindMeToo.Equals(reaction))
                return false;

            var reminders = await ScheduledTasksService.LookupAsync(
                RemindersModule.Discriminator,
                t => t.Data.Contains($@"""messageUrl"":""{message.GetJumpUrl()}"",")
                     && t.Data.Contains($@"""userId"":{user.Id},""")
            );

            var reminder = reminders.SingleOrDefault();
            if (reminder == null)
                return false;

            await ScheduledTasksService.CancelAsync(reminder.ScheduledTaskId);
            return true;
        }
    }
}
