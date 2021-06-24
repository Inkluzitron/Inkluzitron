using Inkluzitron.Extensions;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace Inkluzitron.Models.Settings
{
    public class KisSettings
    {
        public KisSettings(IConfiguration configuration)
        {
            var kis = configuration.GetSection("Kis");
            kis.AssertExists();

            DateTimeFormat = kis.GetRequired<string>(nameof(DateTimeFormat));
            Messages = kis.GetSection("Messages")
                .GetChildren()
                .ToDictionary(o => o.Key, o => o.Value);
            SyncMonths = kis.GetValue<int>(nameof(SyncMonths));
        }

        public string DateTimeFormat { get; }
        public Dictionary<string, string> Messages { get; }
        public int SyncMonths { get; }
    }
}
