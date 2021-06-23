using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Inkluzitron.Services.ChannelLogger
{
    public class ChannelLoggerConfiguration
    {
        public int EventId { get; set; }

        public Dictionary<LogLevel, string> LogLevels { get; set; } = new()
        {
            [LogLevel.Warning] = "⚠ **Warning**",
            [LogLevel.Error] = "🛑 **Error**",
            [LogLevel.Critical] = "🚨 **Critical**"
        };
    }
}
