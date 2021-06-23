using Discord;
using Inkluzitron.Enums;
using Inkluzitron.Extensions;
using Inkluzitron.Models.Settings;
using System;

namespace Inkluzitron.Models
{
    public class BdsmTraitOperationCheck
    {
        private readonly BdsmTraitOperationCheckTranslations _translations;
        private readonly Gender _userGender;
        private readonly Gender _targetGender;

        public BdsmTraitOperationCheck(BdsmTraitOperationCheckTranslations translations,
            Gender userGender, Gender targetGender)
        {
            _translations = translations ?? throw new ArgumentNullException(nameof(translations));
            _userGender = userGender;
            _targetGender = targetGender;
        }

        public string UserDisplayName { get; set; }
        public string TargetDisplayName { get; set; }
        public BdsmTraitOperationCheckResult Result { get; set; }

        public bool Backfired => Result == BdsmTraitOperationCheckResult.RollFailed;
        public bool CanProceedNormally => !Backfired
            && Result != BdsmTraitOperationCheckResult.TargetDidNotConsent
            && Result != BdsmTraitOperationCheckResult.UserDidNotConsent
            && Result != BdsmTraitOperationCheckResult.UserNegativePoints;

        public int UserSubmissiveness { get; set; }
        public int UserDominance { get; set; }
        public int TargetSubmissiveness { get; set; }
        public int TargetDominance { get; set; }

        public int RolledValue { get; set; }
        public int RollMaximum { get; set; }
        public int RequiredValue { get; set; }
        public int PointsToSubtract => Backfired ? RequiredValue - RolledValue : 0;

        public override string ToString()
        {
            string format;

            switch (Result)
            {
                case BdsmTraitOperationCheckResult.UserHasNoTest:
                    return string.Format(_translations.MissingTest, UserDisplayName);

                case BdsmTraitOperationCheckResult.TargetHasNoTest:
                    return string.Format(_translations.MissingTest, TargetDisplayName);

                case BdsmTraitOperationCheckResult.Self:
                    return string.Format(_translations.Self, UserDisplayName);

                case BdsmTraitOperationCheckResult.InCompliance:
                    return string.Format(
                        _translations.InCompliance,
                        UserDisplayName, UserSubmissiveness, UserDominance,
                        TargetDisplayName, TargetSubmissiveness, TargetDominance
                    );

                case BdsmTraitOperationCheckResult.RollSucceeded:
                    format = _translations.RollSucceeded;
                    break;

                case BdsmTraitOperationCheckResult.RollFailed:
                    format = _translations.RollFailed;
                    break;

                case BdsmTraitOperationCheckResult.UserDidNotConsent:
                    return _translations.MissingUserConsentGendered[(int)_userGender];

                case BdsmTraitOperationCheckResult.TargetDidNotConsent:
                    return string.Format(_translations.MissingTargetConsentGendered[(int)_targetGender], TargetDisplayName);

                case BdsmTraitOperationCheckResult.UserNegativePoints:
                    return _translations.NegativePoints;

                default:
                    return base.ToString();
            }

            return string.Format(
                format,
                UserDisplayName, UserSubmissiveness, UserDominance,
                TargetDisplayName, TargetSubmissiveness, TargetDominance,
                RolledValue, RollMaximum, RequiredValue, PointsToSubtract,
                _translations.RollFailedLossGendered[(int)_userGender],
                _translations.RollFailedGainGendered[(int)_targetGender]
            );
        }
    }
}
