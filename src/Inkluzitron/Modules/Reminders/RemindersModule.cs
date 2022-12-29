using System;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Inkluzitron.Data;
using Inkluzitron.Data.Entities;
using Inkluzitron.Models.Settings;
using Inkluzitron.Services;
using Newtonsoft.Json;

namespace Inkluzitron.Modules.Reminders
{
    [Name("Připomenutí")]
    public class RemindersModule : ModuleBase
    {
        private ReminderManager ReminderManager { get; }
        private ReactionSettings ReactionSettings { get; }

        public RemindersModule(ReminderManager reminderManager, ReactionSettings reactionSettings)
        {
            ReminderManager = reminderManager;
            ReactionSettings = reactionSettings;
        }

        [Command("remind"), Alias("remindme", "remind me", "připomeň", "připomeň mi")]
        public async Task RemindMeAsync([Name("kdy")] DateTime when, [Remainder][Name("důvod")] string reason)
        {
            
            await ReplyAsync($"Připomenutí nastaveno. Reakcí {ReactionSettings.RemindMeToo} na původní zprávě budete upozorněni také.");
            await Context.Message.AddReactionAsync(ReactionSettings.RemindMeToo);
        }

        [Command("remind list")]
        public async Task ListRemindersAsync()
        {
            var userId = Context.User.Id;
            var reminders = await ScheduledTasksService.LookupAsync(
                Discriminator,
                t => t.Data.Contains($@"""userId"":{userId},")
            );

            var builder = new StringBuilder();
            foreach (var reminder in reminders)
            {
                var data = reminder.ParseData<Reminder>();
                builder.AppendFormat($"Připomenutí #{reminder.ScheduledTaskId}: {Format.Sanitize(data.Reason)}");
                builder.AppendLine();
                builder.AppendLine(data.MessageUrl); // Message URL
                builder.AppendLine();
                builder.AppendLine();
            }

            if (builder.Length == 0)
            {
                await ReplyAsync("Nemáš žádná nadcházející připomenutí.");
                return;
            }

            await ReplyAsync(builder.ToString());
        }

        [Command("remind cancel")]
        public async Task CancelReminderAsync(long reminderId)
        {
            var reminder = await ScheduledTasksService.LookupAsync(reminderId);
            var isValidReminder = reminder != null && reminder.Discriminator == Discriminator;
            if (!isValidReminder)
            {
                await ReplyAsync("Pod tímto ID neexistuje žádné čekající připomenutí.");
                return;
            }

            var data = reminder.ParseData<Reminder>();
            if (data.UserId != Context.User.Id)
            {
                await ReplyAsync("Toto připomenutí ti nepatří.");
                return;
            }

            await ScheduledTasksService.CancelAsync(reminder.ScheduledTaskId);
            await Context.Message.AddReactionAsync(ReactionSettings.Checkmark);
        }
    }
}
