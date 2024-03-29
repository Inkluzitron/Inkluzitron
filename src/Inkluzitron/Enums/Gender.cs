﻿using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Inkluzitron.Enums
{
    public enum Gender
    {
        [EnumMember(Value = "O")]
        [Display(Name = "neutrální")]
        Unspecified,

        [EnumMember(Value = "M")]
        [Display(Name = "on")]
        Male,

        [EnumMember(Value = "F")]
        [Display(Name = "ona")]
        Female
    }
}
