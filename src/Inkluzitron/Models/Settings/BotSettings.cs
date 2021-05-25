using Microsoft.Extensions.Configuration;

namespace Inkluzitron.Models.Settings
{
    public class BotSettings
    {
        public BotSettings(IConfiguration configuration)
        {
            HomeGuildId = configuration.GetValue<ulong>(nameof(HomeGuildId));
        }

        public ulong HomeGuildId { get; }
    }
}
