using System;

namespace Inkluzitron.Modules.Help
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class DisableStandaloneHelpPageAttribute : Attribute
    {
    }
}
