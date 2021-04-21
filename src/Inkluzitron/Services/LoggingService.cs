using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
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
            var logger = LoggerFactory.CreateLogger(message.Source);

            switch (message.Severity)
            {
                case LogSeverity.Warning when message.Exception == null:
                    logger.LogWarning(message.Message);
                    break;
                case LogSeverity.Warning when message.Exception != null:
                    logger.LogWarning(message.Exception, message.Message);
                    break;
                case LogSeverity.Critical:
                    logger.LogCritical(message.Exception, message.Message);
                    break;
                case LogSeverity.Error:
                    logger.LogError(message.Exception, message.Message);
                    break;
                default:
                    logger.LogInformation(message.Message);
                    break;
            }

            return Task.CompletedTask;
        }
    }
}
