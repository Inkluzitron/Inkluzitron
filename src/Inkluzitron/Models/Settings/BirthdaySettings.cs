using Inkluzitron.Extensions;
using Microsoft.Extensions.Configuration;
using System;

namespace Inkluzitron.Models.Settings
{
    public class BirthdaySettings
    {
        public ulong BirthdayNotificationChannelId { get; }
        public TimeSpan BirthdayNotificationTime { get; }
        public string NoBirthdaysTodayMessage { get; }
        public string BirthdayListHeader { get; }
        public string BirthdayListFooter { get; }
        public string YearsOld { get; }
        public string UnrecognizedBirthdayDateFormatMessage { get; }

        public BirthdaySettings(IConfiguration configuration)
        {
            var cfg = configuration.GetSection("Birthday");
            cfg.AssertExists();

            BirthdayNotificationChannelId = cfg.GetRequired<ulong>(nameof(BirthdayNotificationChannelId));
            BirthdayNotificationTime = cfg.GetRequired<TimeSpan>(nameof(BirthdayNotificationTime));
            NoBirthdaysTodayMessage = cfg.GetRequired<string>(nameof(NoBirthdaysTodayMessage));
            BirthdayListHeader = cfg.GetRequired<string>(nameof(BirthdayListHeader));
            BirthdayListFooter = cfg.GetRequired<string>(nameof(BirthdayListFooter));
            YearsOld = cfg.GetRequired<string>(nameof(YearsOld));
            UnrecognizedBirthdayDateFormatMessage = cfg.GetRequired<string>(nameof(UnrecognizedBirthdayDateFormatMessage));

        }
    }
}
