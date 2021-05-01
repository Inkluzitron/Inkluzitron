using Microsoft.Extensions.Configuration;
using System;
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
            TraitList = new HashSet<string>(config.GetSection($"{SectionName}:Traits").GetChildren().Select(c => c.Value));
            TestLinkUrl = GetRequiredConfig(nameof(TestLinkUrl));
        }
    }
}
