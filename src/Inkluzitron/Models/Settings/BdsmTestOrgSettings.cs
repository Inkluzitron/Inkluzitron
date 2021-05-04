using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
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

        public BdsmTestOrgSettings(IConfiguration config)
        {
            const string SectionName = "BdsmTestOrgQuizModule";
            string GetRequiredConfig(string key)
            {
                key = $"{SectionName}:{key}";
                var result = config.GetValue<string>(key);
                return result ?? throw new InvalidOperationException($"Missing required configuration value with key {key}");
            }

            NoResultsOnRecordMessage = GetRequiredConfig(nameof(NoResultsOnRecordMessage));
            InvalidFormatMessage = GetRequiredConfig(nameof(InvalidFormatMessage));
            LinkAlreadyPresentMessage = GetRequiredConfig(nameof(LinkAlreadyPresentMessage));
            InvalidTraitMessage = GetRequiredConfig(nameof(InvalidTraitMessage));
            InvalidPercentageMessage = GetRequiredConfig(nameof(InvalidPercentageMessage));
            InvalidTraitCountMessage = GetRequiredConfig(nameof(InvalidTraitCountMessage));
            NoMatchesMessage = GetRequiredConfig(nameof(NoMatchesMessage));
            MaximumMatchCount = int.Parse(GetRequiredConfig(nameof(MaximumMatchCount)), CultureInfo.InvariantCulture);
            TraitReportingThreshold = double.Parse(GetRequiredConfig(nameof(TraitReportingThreshold)), CultureInfo.InvariantCulture);
            NoTraitsToReportMessage = GetRequiredConfig(nameof(NoTraitsToReportMessage));
            BadFilterQueryMessage = GetRequiredConfig(nameof(BadFilterQueryMessage));
            TraitList = new HashSet<string>(config.GetSection($"{SectionName}:Traits").GetChildren().Select(c => c.Value));
            TestLinkUrl = GetRequiredConfig(nameof(TestLinkUrl));
        }
    }
}
