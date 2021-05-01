using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inkluzitron.Extensions
{
    static public class CommandExtensions
    {
        static public string GetCommandFormat(this CommandInfo command, string prefix, ParameterInfo highlightArg = null)
        {
            var builder = new StringBuilder().Append(prefix).Append(command.Name);

            foreach (var param in command.Parameters)
            {
                builder.Append(' ');
                if (param == highlightArg) builder.Append("**__");
                builder.Append(param.IsOptional ? "[" : "").Append(param.Summary ?? param.Name).Append(param.IsOptional ? "]" : "");
                if (param == highlightArg) builder.Append("__**");
            }

            return builder.ToString();
        }
    }
}
