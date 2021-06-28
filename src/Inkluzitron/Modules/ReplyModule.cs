using Discord.WebSocket;
using Discord;
using Inkluzitron.Handlers;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Inkluzitron.Modules
{
    public class ReplyModule : ModuleBase
    {
        private DiscordSocketClient DiscordClient { get; }

        public ReplyModule(DiscordSocketClient discordClient)
        {
            DiscordClient = discordClient;
            DiscordClient.MessageReceived += OnMessageReceivedAsync;
        }

        static private bool ContainsPhrase(string message, string regex, bool matchOnlyWord=true)
        {
            if (matchOnlyWord) regex = $"(?<!\\w){regex}(?!\\w)";

            return Regex.IsMatch(message, regex, RegexOptions.IgnoreCase);
        }

        private async Task OnMessageReceivedAsync(SocketMessage message)
        {
            if (!MessagesHandler.TryParseMessageAndCheck(message, out SocketUserMessage userMessage)) return;

            if (ContainsPhrase(message.Content, "uh ?oh"))
            {
                await message.Channel.SendMessageAsync("uh oh");
            }
            else if(ContainsPhrase(message.Content, "oh ?no"))
            {
                await message.Channel.SendMessageAsync("oh no");
            }
            else if (ContainsPhrase(message.Content, "m[aá]m pravdu.*\\?", false))
            {
                await userMessage.ReplyAsync(ThreadSafeRandom.Next(0, 2) == 1 ? "Ano, máš pravdu." : "Ne, nemáš pravdu.", allowedMentions: CheckAndFixAllowedMentions(null));
            }
            else if (ContainsPhrase(message.Content, "^je [cč]erstv[aá]"))
            {
                await message.Channel.SendMessageAsync("Není čerstvá!");
            }
            else if (ContainsPhrase(message.Content, "^nen[ií] [cč]erstv[aá]"))
            {
                await message.Channel.SendMessageAsync("Je čerstvá!");
            }
            else if (ContainsPhrase(message.Content, "^PR$"))
            {
                await message.Channel.SendMessageAsync("https://github.com/Inkluzitron/Inkluzitron");
            }
        }
    }
}
