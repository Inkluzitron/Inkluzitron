using Discord;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Inkluzitron.Services.ChannelLogger
{
    public class ChannelLogger : ILogger
    {
        private readonly string Name;
        private readonly Func<ChannelLoggerConfiguration> GetCurrentConfig;

        public ITextChannel LoggingChannel { get; set; } = null;

        public ChannelLogger(
            string name,
            Func<ChannelLoggerConfiguration> getCurrentConfig,
            ITextChannel loggingChannel)
        {
            Name = name;
            GetCurrentConfig = getCurrentConfig;
            LoggingChannel = loggingChannel;
        }

        public IDisposable BeginScope<TState>(TState state) => default;

        public bool IsEnabled(LogLevel logLevel) =>
            GetCurrentConfig().LogLevels.ContainsKey(logLevel);

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            try
            {
                LogAsync(logLevel, eventId, state, exception, formatter).Wait();
            }
            catch(Exception e)
            {
                Console.Error.WriteLine("Exception occured while trying to send log to Discord channel.");
                Console.Error.WriteLine(e);
            }
        }

        public async Task LogAsync<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel) || LoggingChannel == null)
                return;

            var config = GetCurrentConfig();
            if (config.EventId != 0 && config.EventId != eventId.Id)
                return;

            var errorMessage = formatter(state, exception);
            if (string.IsNullOrEmpty(errorMessage) && exception != null)
                errorMessage = exception.ToString();

            var header = $"{config.LogLevels[logLevel]} {Name}";

            if (string.IsNullOrEmpty(errorMessage))
            {
                await LoggingChannel.SendMessageAsync(header, allowedMentions: AllowedMentions.None);
                return;
            }

            using var fileStream = new MemoryStream();
            fileStream.Write(Encoding.UTF8.GetBytes(errorMessage));
            fileStream.Seek(0, SeekOrigin.Begin);

            await LoggingChannel.SendFileAsync(
                fileStream,
                $"stacktrace-{DateTime.Now:s}.txt",
                header,
                allowedMentions: AllowedMentions.None);
        }
    }
}
