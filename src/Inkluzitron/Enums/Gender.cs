using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Inkluzitron.Enums
{
    public enum Gender
    {
        [EnumMember(Value = "O")]
        [Display(Name = "nezvoleno")]
        Unspecified,

        [EnumMember(Value = "M")]
        [Display(Name = "muž")]
        Male,

        [EnumMember(Value = "F")]
        [Display(Name = "žena")]
        Female
    }
}
