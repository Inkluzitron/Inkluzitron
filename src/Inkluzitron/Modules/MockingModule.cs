using System.IO;
using System.Threading.Tasks;
using Discord.Commands;
using Inkluzitron.Resources.SpongeMock;

namespace Inkluzitron.Modules
{
    public class MockingModule : ModuleBase
    {
        [Command("mock")]
        [Summary("Mockuje zadanou zprávu, nebo zprávu na kterou uživatel reaguje.")]
        public async Task MockAsync(params string[] strings)
        {
            var message = string.Join(" ", strings).ToLower();
            if (message.Length == 0)
            {
                if (Context.Message.ReferencedMessage == null)
                {
                    await ReplyAsync("Chybí zpráva k mockování.");
                    return;
                }

                message = Context.Message.ReferencedMessage.ToString().ToLower();
            }

            var newString = "";
            var toUpper = false;
            foreach (var t in message)
            {
                if (t == ' ')
                {
                    newString += t;
                    continue;
                }

                newString += toUpper ? t.ToString().ToUpper() : t;
                toUpper = !toUpper;
            }

            using var stream = new MemoryStream();
            SpongeMockResources.sponge.Save(stream, System.Drawing.Imaging.ImageFormat.Gif);
            stream.Seek(0, SeekOrigin.Begin);

            await ReplyStreamAsync(stream, "mock.gif", text: newString);
        }
    }
}
