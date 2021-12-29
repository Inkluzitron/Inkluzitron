using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Inkluzitron.Models.Settings;

namespace Inkluzitron.Modules
{
    [Name("Anonymní odesílání zpráv")]
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

            if (roomName.StartsWith('#'))
                roomName = roomName[1..];

            var channel = guild.TextChannels.FirstOrDefault(c => c.Name == roomName);
            if (channel is null || channel.GetUser(Context.User.Id) is null)
            {
                await ReplyAsync(SendSettings.ErrorRoomNotFound);
                return;
            }

            await SendAsync(channel, messageText);
        }


        [Command("send")]
        [Summary("Odešle anonymně zprávu (včetně příloh) do zadané roomky.")]
        public async Task SendAsync([Name("#cílová roomka")] IMessageChannel channel, [Remainder][Name("zpráva")] string messageText = null)
        {
            var attachments = Context.Message.Attachments;
            if (messageText == null && attachments.Count == 0)
            {
                await ReplyAsync(SendSettings.ErrorNoContent);
                return;
            }

            if (!Context.IsPrivate) // DMs have blocked removing reactions.
                await Context.Message.DeleteAsync();

            if (messageText != null)
            {
                await channel.SendMessageAsync(
                    messageText,
                    allowedMentions: new AllowedMentions { AllowedTypes = AllowedMentionTypes.None }
                );
            }

            if (attachments.Count > 0)
            {
                using var client = new HttpClient();

                foreach (var a in attachments)
                {
                    using var response = await client.GetAsync(a.Url);

                    if (!response.IsSuccessStatusCode)
                        continue;

                    var oldExtension = Path.GetExtension(a.Filename);
                    var filename = Guid.NewGuid().ToString("N") + oldExtension;

                    using var stream = await response.Content.ReadAsStreamAsync();
                    await channel.SendFileAsync(stream, filename, string.Empty, isSpoiler: a.IsSpoiler());
                }
            }
        }
    }
}
