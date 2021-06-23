using Discord;
using Discord.WebSocket;
using Inkluzitron.Models.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Inkluzitron.Services.ChannelLogger
{
    public class ChannelLoggerProvider : ILoggerProvider
    {
        private readonly IDisposable OnChangeToken;
        private readonly ConcurrentDictionary<string, ChannelLogger> Loggers = new();

        private ChannelLoggerConfiguration CurrentConfig { get; set; }
        private DiscordSocketClient DiscordClient { get; }
        private BotSettings BotSettings { get; }
        private ITextChannel LoggingChannel { get; set; } = null;

        public ChannelLoggerProvider(
            IOptionsMonitor<ChannelLoggerConfiguration> config, DiscordSocketClient discordClient, BotSettings botSettings)
        {
            CurrentConfig = config.CurrentValue;
            OnChangeToken = config.OnChange(updatedConfig => CurrentConfig = updatedConfig);
            DiscordClient = discordClient;
            BotSettings = botSettings;

            DiscordClient.Ready += DiscordClientReady;
        }

        private Task DiscordClientReady()
        {
            var guild = DiscordClient.GetGuild(BotSettings.HomeGuildId);
            if (guild != null)
            {
                LoggingChannel = guild.GetTextChannel(BotSettings.LoggingChannelId);
                foreach (var logger in Loggers)
                {
                    logger.Value.LoggingChannel = LoggingChannel;
                }
            }

            return Task.CompletedTask;
        }

        public ILogger CreateLogger(string categoryName) =>
            Loggers.GetOrAdd(categoryName, name => new ChannelLogger(name, GetCurrentConfig, LoggingChannel));

        private ChannelLoggerConfiguration GetCurrentConfig() => CurrentConfig;

        public void Dispose()
        {
            Loggers.Clear();
            OnChangeToken.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
