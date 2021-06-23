using Microsoft.Extensions.Configuration;

namespace Inkluzitron.Models.Settings
{
    public class BotSettings
    {
        public BotSettings(IConfiguration configuration)
        {
            LoggingChannelId = configuration.GetValue<ulong>(nameof(LoggingChannelId));
            HomeGuildId = configuration.GetValue<ulong>(nameof(HomeGuildId));
            PointsKarmaIncrement = configuration.GetValue<int>(nameof(PointsKarmaIncrement));
        }

        public ulong LoggingChannelId { get; }
        public ulong HomeGuildId { get; }
        public int PointsKarmaIncrement { get; }
    }
}
