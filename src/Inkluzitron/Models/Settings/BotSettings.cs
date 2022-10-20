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
            UserPointsGraphDays = configuration.GetValue<int>(nameof(UserPointsGraphDays));
            NewbieRoleId = configuration.GetValue<ulong>(nameof(NewbieRoleId));
            NewbieInviteMessage = configuration.GetValue<string>(nameof(NewbieInviteMessage));
        }

        public ulong LoggingChannelId { get; }
        public ulong HomeGuildId { get; }
        public int PointsKarmaIncrement { get; }
        public int UserPointsGraphDays { get; }
        public ulong NewbieRoleId { get; }
        public string NewbieInviteMessage { get; }
    }
}
