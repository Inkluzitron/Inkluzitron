using Discord;
using Inkluzitron.Models.Settings;
using System;

namespace Inkluzitron.Models
{
    public class BdsmTraitOperationCheck
    {
        private readonly BdsmTraitOperationCheckTranslations _translations;

        public BdsmTraitOperationCheck(BdsmTraitOperationCheckTranslations translations)
        {
            _translations = translations ?? throw new ArgumentNullException(nameof(translations));
        }

        public IUser User { get; set; }
        public IUser Target { get; set; }
        public BdsmOperationCheckResult Result { get; set; }
        public bool IsSuccessful => Result != BdsmOperationCheckResult.RollFailed;

        public int UserSubmissiveness { get; set; }
        public int UserDominance { get; set; }
        public int TargetSubmissiveness { get; set; }
        public int TargetDominance { get; set; }

        public int RolledValue { get; set; }
        public int RollMaximum { get; set; }
        public int RequiredValue { get; set; }

        public override string ToString()
        {
            string format;

            switch (Result)
            {
                case BdsmOperationCheckResult.UserHasNoTest:
                    return string.Format(_translations.MissingTest, User.Username);

                case BdsmOperationCheckResult.TargetHasNoTest:
                    return string.Format(_translations.MissingTest, Target.Username);

                case BdsmOperationCheckResult.Self:
                    return string.Format(_translations.Self, User.Username);

                case BdsmOperationCheckResult.InCompliance:
                    return string.Format(
                        _translations.InCompliance,
                        User.Username, UserSubmissiveness, UserDominance,
                        Target.Username, TargetSubmissiveness, TargetDominance
                    );

                case BdsmOperationCheckResult.RollSucceeded:
                    format = _translations.RollSucceeded;
                    break;

                case BdsmOperationCheckResult.RollFailed:
                    format = _translations.RollFailed;
                    break;

                default:
                    return base.ToString();
            }

            return string.Format(
                format,

                User.Username, UserSubmissiveness, UserDominance,
                Target.Username, TargetSubmissiveness, TargetDominance,
                RolledValue, RollMaximum, RequiredValue
            );
        }
    }
}
