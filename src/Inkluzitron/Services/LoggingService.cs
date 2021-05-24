using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Inkluzitron.Services
{
    public class LoggingService
    {
        private DiscordSocketClient DiscordClient { get; }
        private CommandService CommandService { get; }
        private ILoggerFactory LoggerFactory { get; }

        public LoggingService(DiscordSocketClient discord, CommandService commandService, ILoggerFactory loggerFactory)
        {
            DiscordClient = discord;
            CommandService = commandService;
            LoggerFactory = loggerFactory;

            DiscordClient.Log += OnLogAsync;
            CommandService.Log += OnLogAsync;
        }

        private Task OnLogAsync(LogMessage message)
        {
            try
            {
                var logger = LoggerFactory.CreateLogger(message.Source);

                var messageText = message.Message;
                if(messageText != null)
                {
                    messageText = Regex.Replace(
                        messageText,
                        @"^(Executed \""send\"").+",
                        "$1"
                    );
                }

                switch (message.Severity)
                {
                    case LogSeverity.Warning when message.Exception == null:
                        logger.LogWarning(messageText);
                        break;
                    case LogSeverity.Warning when message.Exception != null:
                        logger.LogWarning(message.Exception, messageText);
                        break;
                    case LogSeverity.Critical:
                        logger.LogCritical(message.Exception, messageText);
                        break;
                    case LogSeverity.Error:
                        logger.LogError(message.Exception, messageText);
                        break;
                    default:
                        logger.LogInformation(messageText);
                        break;
                }
            }
            catch (ObjectDisposedException)
            {
                // Cannot create logger. Use standard console.

                if (message.Severity == LogSeverity.Error || message.Severity == LogSeverity.Critical)
                    Console.Error.WriteLine(message.ToString());
                else
                    Console.WriteLine(message.ToString());
            }

            return Task.CompletedTask;
        }
    }
}
