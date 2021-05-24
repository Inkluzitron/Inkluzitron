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
        private SendSettings SendSettings { get; }

        public SendModule(SendSettings sendSettings)
        {
            SendSettings = sendSettings;
        }

        [Command("send")]
        [Summary("Odešle anonymně zprávu (včetně příloh) do zadané roomky. Funguje pouze v DM.")]
        public async Task SendAsync([Name("název roomky")] string roomName, [Remainder][Name("zpráva")] string messageText = null)
        {
            if (!Context.IsPrivate)
            {
                await ReplyAsync(SendSettings.ErrorDmOnly);
                return;
            }

            var guild = Context.Client.Guilds.FirstOrDefault(g => g.Id == SendSettings.GuildId);
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
