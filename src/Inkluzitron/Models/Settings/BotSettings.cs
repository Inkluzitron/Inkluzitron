using Microsoft.Extensions.Configuration;

namespace Inkluzitron.Models.Settings
{
    public class BotSettings
    {
        public BotSettings(IConfiguration configuration)
        {
            HomeGuildId = configuration.GetValue<ulong>(nameof(HomeGuildId));
            PointsKarmaIncrement = configuration.GetValue<int>(nameof(PointsKarmaIncrement));
        }

        public ulong HomeGuildId { get; }
        public int PointsKarmaIncrement { get; }
    }
}
