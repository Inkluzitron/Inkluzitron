using System;
using System.ComponentModel.DataAnnotations;

namespace Inkluzitron.Enums
{
    [Flags]
    public enum CommandConsent
    {
        None = 0,

        [Display(Name = "BDSM Image Commands")]
        BdsmImageCommands = 1 << 0
    }
}
