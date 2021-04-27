using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;

namespace Inkluzitron.Modules
{

    public class MockingModule : ModuleBase
    {
        private IConfiguration Config { get; }

        public MockingModule(IConfiguration config)
        {
            Config = config;
        }

        [Command("mock")]
        [Summary("Mockuje zadanou zprávu, nebo zprávu na kterou uživatel reaguje.")]
        public async Task MockAsync(params string[] strings)
        {
            var message = string.Join(" ", strings).ToLower();
            var hasReferencedMsg = false;
            if (message.Length == 0)
            {
                if (Context.Message.ReferencedMessage == null)
                {
                    await ReplyAsync("Chybí zpráva k mockování.");
                    return;
                }

                hasReferencedMsg = true;
                message = Context.Message.ReferencedMessage.ToString().ToLower();
            }

            var newString = "";
            var rnd = new Random(int.Parse(DateTimeOffset.Now.ToUnixTimeSeconds().ToString()));
            var lastDigit = int.Parse(rnd.Next().ToString().Last().ToString());
            var toUpper = lastDigit >= 5;
            foreach (var t in message)
            {
                // add special chars and letter 'i' without changing toUpper variable
                if (t < 97 || t > 122 || t == 'i')
                {
                    newString += t;
                    continue;
                }
                // add letter 'l' casted to uppercase without changing toUpper variable
                if (t == 'l')
                {
                    newString += t.ToString().ToUpper();
                    continue;
                }


                newString += toUpper ? t.ToString().ToUpper() : t;

                // get new random number that decides whether we should change to upper/lower
                var tmp = int.Parse(rnd.Next().ToString().Last().ToString());
                toUpper = tmp >= 3 ? !toUpper : toUpper;
            }

            // if mocking of referenced message don't use prepared ReplyFileAsync function because we want to reply to
            // author of referenced message instead of replying to mocker
            if (hasReferencedMsg)
            {
                var am = new AllowedMentions() {MentionRepliedUser = true};
                var mr = new MessageReference(Context.Message.ReferencedMessage.Id, Context.Channel.Id, Context.Guild.Id);

                await Context.Channel.SendFileAsync(
                    "./Assets/mockingSponge.gif",
                    text: newString,
                    isTTS: false,
                    embed: null,
                    options: RequestOptions.Default,
                    isSpoiler: false,
                    allowedMentions: am,
                    messageReference: mr
                );
            }
            else
                await ReplyFileAsync(Config["Spongebob"], newString);
        }
    }
}
