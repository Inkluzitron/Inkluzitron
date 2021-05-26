using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Inkluzitron.Models.Settings;

namespace Inkluzitron.Modules
{
    [Name("Odesílání zpráv")]
    public class SendModule : ModuleBase
    {
        private BotSettings BotSettings { get; }
        private SendSettings SendSettings { get; }

        public SendModule(BotSettings botSettings, SendSettings sendSettings)
        {
            BotSettings = botSettings;
            SendSettings = sendSettings;
        }

        [Command("send")]
        [Summary("Odešle anonymně zprávu (včetně příloh) do zadané roomky.")]
        public async Task SendAsync([Name("cílová roomka")] string roomName, [Remainder][Name("zpráva")] string messageText = null)
        {
            var guild = Context.Client.GetGuild(BotSettings.HomeGuildId);
            if (guild is null)
            {
                await ReplyAsync(SendSettings.ErrorGuildNotFound);
                return;
            }

            var channel = guild.TextChannels.FirstOrDefault(c => c.Name == roomName);
            if (channel is null || channel.GetUser(Context.User.Id) is null)
            {
                await ReplyAsync(SendSettings.ErrorRoomNotFound);
                return;
            }

            var attachments = Context.Message.Attachments;
            if (messageText == null && attachments.Count == 0)
            {
                await ReplyAsync(SendSettings.ErrorNoContent);
                return;
            }

            if (!Context.IsPrivate) // DMs have blocked removing reactions.
                await Context.Message.DeleteAsync();

            if (messageText != null)
                await channel.SendMessageAsync(messageText);

            if (attachments.Count > 0)
            {
                using var client = new HttpClient();

                foreach (var a in attachments)
                {
                    using var response = await client.GetAsync(a.Url);

                    if (!response.IsSuccessStatusCode)
                        continue;

                    using var stream = await response.Content.ReadAsStreamAsync();
                    await channel.SendFileAsync(stream, a.Filename, messageText ?? string.Empty, isSpoiler: a.IsSpoiler());
                }
            }
        }
    }
}
