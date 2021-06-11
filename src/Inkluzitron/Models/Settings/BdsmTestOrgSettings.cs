using Inkluzitron.Extensions;
using Microsoft.Extensions.Configuration;

namespace Inkluzitron.Models.Settings
{
    public class BdsmTestOrgSettings
    {
        public BdsmTestOrgApiKey ApiKey { get; }
        public string NoResultsOnRecordMessage { get; }
        public string InvalidFormatMessage { get; }
        public string LinkAlreadyPresentMessage { get; }

        public string NoMatchesMessage { get; }
        public int MaximumMatchCount { get; }
        public double StrongTraitThreshold { get; }
        public double WeakTraitThreshold { get; }
        public string NoTraitsToReportMessage { get; }
        public string BadFilterQueryMessage { get; }
        public string TestLinkUrl { get; }
        public string NoContentToStats { get; }
        public string ConsentRegistered { get; }
        public string ConsentNotRegistered { get; }

        public BdsmTestOrgSettings(IConfiguration config)
        {
            var cfg = config.GetSection("BdsmTestOrgQuizModule");
            cfg.AssertExists();

            ApiKey = cfg.GetRequired<BdsmTestOrgApiKey>(nameof(ApiKey));
            NoResultsOnRecordMessage = cfg.GetRequired<string>(nameof(NoResultsOnRecordMessage));
            InvalidFormatMessage = cfg.GetRequired<string>(nameof(InvalidFormatMessage));
            LinkAlreadyPresentMessage = cfg.GetRequired<string>(nameof(LinkAlreadyPresentMessage));
            NoMatchesMessage = cfg.GetRequired<string>(nameof(NoMatchesMessage));
            MaximumMatchCount = cfg.GetRequired<int>(nameof(MaximumMatchCount));
            StrongTraitThreshold = cfg.GetRequired<double>(nameof(StrongTraitThreshold));
            WeakTraitThreshold = cfg.GetRequired<double>(nameof(WeakTraitThreshold));
            NoTraitsToReportMessage = cfg.GetRequired<string>(nameof(NoTraitsToReportMessage));
            BadFilterQueryMessage = cfg.GetRequired<string>(nameof(BadFilterQueryMessage));
            TestLinkUrl = cfg.GetRequired<string>(nameof(TestLinkUrl));
            NoContentToStats = cfg.GetRequired<string>(nameof(NoContentToStats));
            ConsentRegistered = cfg.GetRequired<string>(nameof(ConsentRegistered));
            ConsentNotRegistered = cfg.GetRequired<string>(nameof(ConsentNotRegistered));
        }
    }
}
