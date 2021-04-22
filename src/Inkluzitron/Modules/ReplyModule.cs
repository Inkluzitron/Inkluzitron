using Discord.WebSocket;
using Inkluzitron.Handlers;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Inkluzitron.Modules
{
    public class ReplyModule : ModuleBase
    {
        private DiscordSocketClient DiscordClient { get; }
        private Random Random { get; }

        public ReplyModule(DiscordSocketClient discordClient, Random random)
        {
            DiscordClient = discordClient;
            Random = random;

            DiscordClient.MessageReceived += OnMessageReceivedAsync;
        }

        private async Task OnMessageReceivedAsync(SocketMessage message)
        {
            if (!MessagesHandler.TryParseMessageAndCheck(message, out SocketUserMessage _)) return;

            if (Regex.IsMatch(message.Content, "uh ?oh", RegexOptions.IgnoreCase))
            {
                await message.Channel.SendMessageAsync("uh oh");
            }
            else if(Regex.IsMatch(message.Content, "oh ?no", RegexOptions.IgnoreCase))
            {
                await message.Channel.SendMessageAsync("oh no");
            }
            else if (Regex.IsMatch(message.Content, "m[aá]m pravdu.*\\?", RegexOptions.IgnoreCase))
            {
                await ReplyAsync(Random.Next(0, 2) == 1 ? "Ano, máš pravdu." : "Ne, nemáš pravdu.");
            }
            else if (Regex.IsMatch(message.Content, "^je [cč]erstv[aá]", RegexOptions.IgnoreCase))
            {
                await message.Channel.SendMessageAsync("Není čerstvá!");
            }
            else if (Regex.IsMatch(message.Content, "^nen[ií] [cč]erstv[aá]", RegexOptions.IgnoreCase))
            {
                await message.Channel.SendMessageAsync("Je čerstvá!");
            }
            else if (Regex.IsMatch(message.Content, "^PR$", RegexOptions.IgnoreCase))
            {
                await message.Channel.SendMessageAsync("https://github.com/Misha12/Inkluzitron");
            }
        }
    }
}
