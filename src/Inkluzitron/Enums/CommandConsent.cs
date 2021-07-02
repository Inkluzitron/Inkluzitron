using System;
using System.ComponentModel.DataAnnotations;

namespace Inkluzitron.Enums
{
    [Flags]
    public enum CommandConsent
    {
        None = 0,

        [Display(
            Name = "BDSM Obrázkové příkazy",
            Description = "s používáním obrázkových BDSM příkazů")]
        BdsmImageCommands = 1 << 0
    }
}
