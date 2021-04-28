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
        private Random Random { get; }

        public MockingModule(IConfiguration config, Random random)
        {
            Config = config;
            Random = random;
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
            var lastDigit = Random.Next(0, 10);
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
                var tmp = Random.Next(0, 10);
                toUpper = tmp >= 3 ? !toUpper : toUpper;
            }

            // if mocking of referenced message don't use prepared ReplyFileAsync function because we want to reply to
            // author of referenced message instead of replying to mocker
            if (hasReferencedMsg)
            {
                var am = new AllowedMentions() {MentionRepliedUser = true};
                var mr = new MessageReference(Context.Message.ReferencedMessage.Id, Context.Channel.Id, Context.Guild.Id);

                await Context.Channel.SendFileAsync(
                    Config["Spongebob"],
                    newString,
                    options: RequestOptions.Default,
                    allowedMentions: am,
                    messageReference: mr
                );
            }
            else
                await ReplyFileAsync(Config["Spongebob"], newString);
        }
    }
}
