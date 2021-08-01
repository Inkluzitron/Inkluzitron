using System;
using System.ComponentModel.DataAnnotations;

namespace Inkluzitron.Enums
{
    [Flags]
    public enum CommandConsent
    {
        None = 0,

        [Display(Name = "Souhlas se vším")]
        All = ~0,

        [Display(
            Name = "Spicy obrázkové příkazy",
            Description = "s používáním spicy obrázkových příkazů")]
        BdsmImageCommands = 1 << 0,

        [Display(
            Name = "Zobrazení odznaků",
            Description = "se zobrazováním získaných odznaků")]
        ShowBadges = 1 << 1
    }
}
