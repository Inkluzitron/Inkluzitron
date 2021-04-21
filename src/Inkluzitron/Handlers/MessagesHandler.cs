using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace Inkluzitron.Handlers
{
    /// <summary>
    /// Handler to catch events about messages (MessageReceived, ...).
    /// </summary>
    public class MessagesHandler : IHandler
    {
        private DiscordSocketClient DiscordClient { get; }
        private CommandService CommandService { get; }
        private IServiceProvider ServiceProvider { get; }
        private IConfiguration Configuration { get; }

        public MessagesHandler(DiscordSocketClient discordClient, CommandService commandService, IServiceProvider serviceProvider,
            IConfiguration configuration)
        {
            DiscordClient = discordClient;
            CommandService = commandService;
            ServiceProvider = serviceProvider;
            Configuration = configuration;

            DiscordClient.MessageReceived += OnMessageReceivedAsync;
        }

        private async Task OnMessageReceivedAsync(SocketMessage message)
        {
            if (!TryParseMessageAndCheck(message, out SocketUserMessage userMessage)) return;

            var context = new SocketCommandContext(DiscordClient, userMessage);

            int argPos = 0;
            if (IsCommand(userMessage, ref argPos))
                await CommandService.ExecuteAsync(context, userMessage.Content[argPos..], ServiceProvider);
        }

        static private bool TryParseMessageAndCheck(SocketMessage message, out SocketUserMessage socketUserMessage)
        {
            socketUserMessage = null;

            if (message is not SocketUserMessage userMessage || message.Author.IsBot || message.Author.IsBot)
                return false;

            socketUserMessage = userMessage;
            return true;
        }

        private bool IsCommand(SocketUserMessage message, ref int argPos)
        {
            if (message.HasMentionPrefix(DiscordClient.CurrentUser, ref argPos))
                return true;

            var prefix = Configuration["Prefix"];
            return message.Content.Length > prefix.Length && message.HasStringPrefix(prefix, ref argPos);
        }
    }
}
