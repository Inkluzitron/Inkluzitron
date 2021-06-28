using Discord.Commands;
using System.Linq;
using System.Text;

namespace Inkluzitron.Extensions
{
    static public class CommandExtensions
    {
        static public string GetAliasesFormat(this CommandInfo command, string prefix)
        {
            var aliases = command.Aliases.Select(a => $"{prefix}{a}").Skip(1);
            return string.Join(", ", aliases);
        }

        static public string GetCommandFormat(this CommandInfo command, string prefix, ParameterInfo highlightArg = null)
        {
            var builder = new StringBuilder(prefix);
            builder.Append(command.Aliases.FirstOrDefault());

            foreach (var param in command.Parameters)
            {
                if (string.IsNullOrEmpty(param.Name)) continue;

                builder.Append(' ');
                if (param == highlightArg) builder.Append("**__");
                builder.Append('`');
                builder.Append(param.Name).Append(param.IsOptional ? "?" : "");
                builder.Append('`');
                if (param == highlightArg) builder.Append("__**");
            }

            return builder.ToString();
        }
    }
}
