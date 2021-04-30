using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Inkluzitron.Modules
{
    public class SendModule : ModuleBase
    {
        [Command("send")]
        [Summary("Odešle zprávu popř. přílohu místo uživetele do channelu a uživatelovu zprávu smaže")]
        public async Task SendAsync([Remainder] string message = null)
        {
            // get collection of message attachements
            var attachments = Context.Message.Attachments;

            // send user to hell if no msg/attachemnt is present
            if (message == null && attachments.Count == 0)
            {
                await ReplyAsync("Tak hele de:b:ílku, tahle by to teda nešlo...\n Dej mi aspoň message nebo file!");
                return;
            }

            // delete user's message
            await Context.Message.DeleteAsync();
            // send message if not null
            if (message != null) await Context.Channel.SendMessageAsync(message);
            // return if no attachment is present
            if (attachments.Count == 0) return;

            // get link to attachments and repost them.
            using var client = new HttpClient();
            foreach (var a in attachments)
            {
                using var response = await client.GetAsync(a.Url);

                if (!response.IsSuccessStatusCode) continue;

                var stream = await response.Content.ReadAsStreamAsync();
                await Context.Channel.SendFileAsync(stream, a.Filename, isSpoiler: a.IsSpoiler());
            }
        }
    }
}
