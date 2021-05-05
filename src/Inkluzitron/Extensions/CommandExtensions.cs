using Discord.Commands;
using System.Collections.Generic;
using System.Text;

namespace Inkluzitron.Extensions
{
    static public class CommandExtensions
    {
        static public string GetCommandFormat(this CommandInfo command, string prefix, ParameterInfo highlightArg = null)
        {
            var builder = new StringBuilder(prefix);

            var groups = new Stack<string>();
            var module = command.Module;
            while (module != null)
            {
                if (module.Group != null)
                    groups.Push(module.Group);

                module = module.Parent;
            }

            while (groups.TryPop(out var groupName))
                builder.Append(groupName).Append(' ');

            builder.Append(command.Name);

            foreach (var param in command.Parameters)
            {
                builder.Append(' ');
                if (param == highlightArg) builder.Append("**__");
                builder.Append(param.IsOptional ? "[" : "").Append(param.Summary ?? param.Name).Append(param.IsOptional ? "]" : "");
                if (param == highlightArg) builder.Append("__**");
            }

            return builder.ToString().Trim();
        }
    }
}
