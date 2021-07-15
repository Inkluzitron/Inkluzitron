using Inkluzitron.Extensions;
using Microsoft.Extensions.Configuration;

namespace Inkluzitron.Models.Settings
{
    public class VoteWinnersTranslations
    {
        public string DeadlineNotice { get; }
        public string NoWinners { get; }
        public string OneWinner { get; }
        public string MultipleWinners { get; }

        public VoteWinnersTranslations(IConfigurationSection configSection)
        {
            DeadlineNotice = configSection.GetRequired<string>(nameof(DeadlineNotice));
            NoWinners = configSection.GetRequired<string>(nameof(NoWinners));
            OneWinner = configSection.GetRequired<string>(nameof(OneWinner));
            MultipleWinners = configSection.GetRequired<string>(nameof(MultipleWinners));
        }
    }
}
