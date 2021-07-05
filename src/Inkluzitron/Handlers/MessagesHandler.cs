using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Inkluzitron.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
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

        public string CommandPrefix { get; }

        public MessagesHandler(DiscordSocketClient discordClient, CommandService commandService, IServiceProvider serviceProvider, IConfiguration configuration)
        {
            DiscordClient = discordClient;
            CommandService = commandService;
            ServiceProvider = serviceProvider;
            CommandPrefix = configuration["Prefix"];

            DiscordClient.MessageReceived += OnMessageReceivedAsync;
        }

        private async Task OnMessageReceivedAsync(SocketMessage message)
        {
            if (!TryParseMessageAndCheck(message, out SocketUserMessage userMessage)) return;

            var context = new SocketCommandContext(DiscordClient, userMessage);

            int argPos = 0;
            if (IsCommand(userMessage, ref argPos))
            {
                await CommandService.ExecuteAsync(context, userMessage.Content[argPos..], ServiceProvider);
            }
            else
            {
                if (!context.IsPrivate)
                    await ServiceProvider.GetRequiredService<PointsService>().IncrementAsync(message);
            }
        }

        static public bool TryParseMessageAndCheck(SocketMessage message, out SocketUserMessage socketUserMessage)
        {
            socketUserMessage = null;

            if (message is not SocketUserMessage userMessage || message.Author.IsBot || message.Author.IsBot)
                return false;

            socketUserMessage = userMessage;
            return true;
        }

        private bool IsCommand(IUserMessage message, ref int argPos)
            => message.HasMentionPrefix(DiscordClient.CurrentUser, ref argPos) || HasCommandPrefix(message, ref argPos);

        private bool HasCommandPrefix(IUserMessage message, ref int argPos)
            => message.Content.Length > CommandPrefix.Length && message.HasStringPrefix(CommandPrefix, ref argPos);

        public async Task<(bool success, string commandName, string commandArguments)> TryMatchSingleCommand(IUserMessage message)
        {
            int argPos = default;
            if (!IsCommand(message, ref argPos))
                return (false, null, null);

            var postPrefixContent = message.Content[argPos..];
            var searchResult = CommandService.Search(postPrefixContent);

            if (!searchResult.IsSuccess)
                return (false, null, null);

            if (searchResult.Commands.Count != 1)
                return (false, null, null);

            var match = searchResult.Commands.SingleOrDefault();
            var parse = await match.ParseAsync(new CommandContext(DiscordClient, message), searchResult, services: ServiceProvider);
            if (!parse.IsSuccess)
                return (false, null, null);

            return (true, match.Alias, parse.ArgValues[0].BestMatch.ToString());
        }
    }
}
