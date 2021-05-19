using Discord.Commands;
using Inkluzitron.Modules.Help;
using System.Linq;

namespace Inkluzitron.Extensions
{
    static public class ModuleInfoExtensions
    {
        static public bool HasStandaloneHelpPage(this ModuleInfo mod)
            => mod.Attributes.All(a => a is not DisableStandaloneHelpPageAttribute _);
    }
}
