using System.Runtime.Serialization;

namespace Inkluzitron.Enums
{
    public enum Gender
    {
        [EnumMember(Value = "O")]
        Unspecified,
        [EnumMember(Value = "M")]
        Male,
        [EnumMember(Value = "F")]
        Female
    }
}
