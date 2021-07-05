using Inkluzitron.Extensions;
using Microsoft.Extensions.Configuration;

namespace Inkluzitron.Models.Settings
{
    public class VoteTranslations
    {
        public VoteWinnersTranslations VoteUnderway { get; }
        public VoteWinnersTranslations VoteFinished { get; }
        public string UnaccessibleEmotes { get; }
        public string DuplicateOption { get; }
        public string LineParseError { get; }
        public string DuplicateDeadline { get; }
        public string DeadlineParseError { get; }
        public string NoOptions { get; }
        public string NoQuestion { get; }

        public VoteTranslations(IConfiguration config)
        {
            var section = config.GetSection(nameof(VoteTranslations));
            VoteUnderway = new VoteWinnersTranslations(section.GetSection(nameof(VoteUnderway)));
            VoteFinished = new VoteWinnersTranslations(section.GetSection(nameof(VoteFinished)));
            UnaccessibleEmotes = section.GetRequired<string>(nameof(UnaccessibleEmotes));
            DuplicateOption = section.GetRequired<string>(nameof(DuplicateOption));
            LineParseError = section.GetRequired<string>(nameof(LineParseError));
            DuplicateDeadline = section.GetRequired<string>(nameof(DuplicateDeadline));
            DeadlineParseError = section.GetRequired<string>(nameof(DeadlineParseError));
            NoOptions = section.GetRequired<string>(nameof(NoOptions));
            NoQuestion = section.GetRequired<string>(nameof(NoQuestion));
        }
    }
}
