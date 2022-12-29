using Discord;
using Discord.Commands;
using Inkluzitron.Extensions;
using Inkluzitron.Models.Reminders;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Inkluzitron.Modules.Reminders
{
    public class ReminderEmbed : EmbedBuilder
    {
        public ReminderEmbed WithData(ReminderEmbedPage reminderEmbedPage, IGuildUser guildUser)
        {
            WithTitle($"Připomenutí pro {guildUser.Nickname}");
            WithColor(new Color(100, 100, 100));
            WithCurrentTimestamp();
            WithAuthor(new EmbedAuthorBuilder()
            {
                Name = "Připomenutí",
                IconUrl = guildUser.GetAvatarUrl()
            });

            if (!reminderEmbedPage.IsEmpty)
                WithFooter($"{reminderEmbedPage.PageNumber}/{reminderEmbedPage.PageCount}");
          
            this.WithMetadata(new ReminderEmbedMetadata {
                UserId = guildUser.Id,
                ReminderId = reminderEmbedPage.Reminder?.ReminderId ?? 0,
                When = reminderEmbedPage.Reminder?.When ?? DateTimeOffset.UnixEpoch
            });

            if (reminderEmbedPage.Reminder is {} reminder)
            {
                var secondsSinceUnixEpoch = (int)Math.Round(reminder.When.Subtract(DateTimeOffset.UnixEpoch).TotalSeconds);
                AddField("Popis", reminder.Reason);
                AddField("Čas upozornění", $"<t:{secondsSinceUnixEpoch}:F>");
            }
            else
            {
                AddField("Nic k vidění", "Nemas zadne reminders.");
            }

            return this;
        }
    }
}
