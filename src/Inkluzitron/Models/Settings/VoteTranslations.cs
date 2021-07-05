using Inkluzitron.Extensions;
using Microsoft.Extensions.Configuration;

namespace Inkluzitron.Models.Settings
{
    public class VoteTranslations
    {
        public VoteWinnersTranslations VoteUnderway { get; }
        public VoteWinnersTranslations VoteFinished { get; }
        public string UnaccessibleEmotes { get; }

        public VoteTranslations(IConfiguration config)
        {
            var section = config.GetSection(nameof(VoteTranslations));
            VoteUnderway = new VoteWinnersTranslations(section.GetSection(nameof(VoteUnderway)));
            VoteFinished = new VoteWinnersTranslations(section.GetSection(nameof(VoteFinished)));
            UnaccessibleEmotes = section.GetRequired<string>(nameof(UnaccessibleEmotes));
        }
    }
}
