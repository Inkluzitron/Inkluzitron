using Discord.WebSocket;
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
            if (!Regex.IsMatch(message.Content, "m[aá]m pravdu", RegexOptions.IgnoreCase)) return;

            if (Random.Next(0, 2) == 1)
                await message.Channel.SendMessageAsync("Ano, máš pravdu.");
            else
                await message.Channel.SendMessageAsync("Ne, nemáš pravdu.");
        }
    }
}
