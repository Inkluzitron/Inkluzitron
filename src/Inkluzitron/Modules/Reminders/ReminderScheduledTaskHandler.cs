using Discord.WebSocket;
using Inkluzitron.Contracts;
using Inkluzitron.Data.Entities;
using Inkluzitron.Enums;
using Inkluzitron.Models.Settings;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Inkluzitron.Modules.Reminders
{
    public class ReminderScheduledTaskHandler : IScheduledTaskHandler
    {
        private DiscordSocketClient Client { get; }
        private BotSettings BotSettings { get; }

        public ReminderScheduledTaskHandler(DiscordSocketClient client, BotSettings botSettings)
        {
            Client = client;
            BotSettings = botSettings;
        }

        public async Task<ScheduledTaskResult> HandleAsync(ScheduledTask scheduledTask)
        {
            if (scheduledTask.Discriminator != RemindersModule.Discriminator)
                return ScheduledTaskResult.NotHandled;

            var data = scheduledTask.ParseData<Reminder>();
            var guild = Client.GetGuild(BotSettings.HomeGuildId);
            if (guild == null)
                throw new InvalidOperationException("Could not locate home guild");

            await guild.DownloadUsersAsync();
            var guildMember = guild.GetUser(data.UserId);
            if (guildMember == null)
                return ScheduledTaskResult.HandledAndCompleted;

            var messageBuilder = new StringBuilder();

            messageBuilder.AppendFormat("Upozornění #{0}: {1}", scheduledTask.ScheduledTaskId, Discord.Format.Sanitize(data.Reason));
            messageBuilder.AppendLine();

            messageBuilder.AppendFormat("Termín: {0}", scheduledTask.When.ToString("G"));
            messageBuilder.AppendLine();

            messageBuilder.AppendFormat("Zpráva: {0}", data.MessageUrl);
            messageBuilder.AppendLine();

            try
            {
                var dmChannel = await guildMember.GetOrCreateDMChannelAsync();
                await dmChannel.SendMessageAsync(messageBuilder.ToString());
                return ScheduledTaskResult.HandledAndCompleted;
            }
            catch (Discord.Net.HttpException e) when (e.HttpCode == System.Net.HttpStatusCode.Forbidden)
            {
                return ScheduledTaskResult.HandledAndCompleted;
            }
        }
    }
}
