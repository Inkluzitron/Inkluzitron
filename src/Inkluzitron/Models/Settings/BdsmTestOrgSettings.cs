using Inkluzitron.Extensions;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace Inkluzitron.Models.Settings
{
    public class BdsmTestOrgSettings
    {
        public string NoResultsOnRecordMessage { get; }
        public string InvalidFormatMessage { get; }
        public string LinkAlreadyPresentMessage { get; }
        public string InvalidTraitMessage { get; }
        public string InvalidPercentageMessage { get; }
        public string InvalidTraitCountMessage { get; }
        public string NoMatchesMessage { get; }
        public int MaximumMatchCount { get; }
        public double TraitReportingThreshold { get; }
        public string NoTraitsToReportMessage { get; }
        public string InvalidVerboseModeUsage { get; }
        public string BadFilterQueryMessage { get; }
        public IReadOnlySet<string> TraitList { get; }
        public string TestLinkUrl { get; }

        public string SwitchTraitName { get; }
        public IReadOnlySet<string> SwitchFilterTraitNames { get; }

        public BdsmTestOrgSettings(IConfiguration config)
        {
            var cfg = config.GetSection("BdsmTestOrgQuizModule");
            cfg.AssertExists();

            NoResultsOnRecordMessage = cfg.GetRequired<string>(nameof(NoResultsOnRecordMessage));
            InvalidFormatMessage = cfg.GetRequired<string>(nameof(InvalidFormatMessage));
            LinkAlreadyPresentMessage = cfg.GetRequired<string>(nameof(LinkAlreadyPresentMessage));
            InvalidTraitMessage = cfg.GetRequired<string>(nameof(InvalidTraitMessage));
            InvalidPercentageMessage = cfg.GetRequired<string>(nameof(InvalidPercentageMessage));
            InvalidTraitCountMessage = cfg.GetRequired<string>(nameof(InvalidTraitCountMessage));
            NoMatchesMessage = cfg.GetRequired<string>(nameof(NoMatchesMessage));
            MaximumMatchCount = cfg.GetRequired<int>(nameof(MaximumMatchCount));
            TraitReportingThreshold = cfg.GetRequired<double>(nameof(TraitReportingThreshold));
            NoTraitsToReportMessage = cfg.GetRequired<string>(nameof(NoTraitsToReportMessage));
            BadFilterQueryMessage = cfg.GetRequired<string>(nameof(BadFilterQueryMessage));
            TestLinkUrl = cfg.GetRequired<string>(nameof(TestLinkUrl));

            var traitsSection = cfg.GetSection("Traits");
            traitsSection.AssertExists();
            TraitList = new HashSet<string>(traitsSection.GetChildren().Select(c => c.Value));
        }
    }
}
