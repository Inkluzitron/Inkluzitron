using Microsoft.Extensions.Configuration;

namespace Inkluzitron.Models.Settings
{
    public class BdsmTraitOperationCheckTranslations
    {
        public BdsmTraitOperationCheckTranslations(IConfiguration config)
        {
            config.GetSection(nameof(BdsmTraitOperationCheckTranslations)).Bind(this);
        }

        public string[] MissingUserConsentGendered { get; set; }
        public string[] MissingTargetConsentGendered { get; set; }
        public string NegativePoints { get; set; }
        public string MissingTest { get; set; }
        public string InCompliance { get; set; }
        public string RollSucceeded { get; set; }
        public string RollFailed { get; set; }
        public string[] RollFailedLossGendered { get; set; }
        public string[] RollFailedGainGendered { get; set; }
        public string Self { get; set; }
    }
}
