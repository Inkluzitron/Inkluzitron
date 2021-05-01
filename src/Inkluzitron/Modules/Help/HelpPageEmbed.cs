using Discord;
using Discord.Commands;
using Inkluzitron.Extensions;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Inkluzitron.Modules.Help
{
    public class HelpPageEmbed : EmbedBuilder
    {
        static private readonly Regex FooterPattern = new Regex(@"^Nápověda\s*\|\s*(\d*)\/(\d*)", RegexOptions.IgnoreCase);

        // Tuple<Page, TotalPages>
        static public Tuple<int, int> TryParseFooter(string footer)
        {
            var match = FooterPattern.Match(footer);
            if (!match.Success)
                return null;

            return Tuple.Create(
                Convert.ToInt32(match.Groups[1].Value), // Page
                Convert.ToInt32(match.Groups[2].Value) // Total pages
            );
        }

        public async Task<HelpPageEmbed> WithModuleAsync(ModuleInfo module, ICommandContext context, IServiceProvider provider, int pagesCount, string prefix,
            int page = 1)
        {
            WithTitle(module.Name);
            WithCurrentTimestamp();
            this.WithAuthor(context.User);
            WithFooter($"Nápověda | {page}/{pagesCount}");

            var executableCommands = await module.GetExecutableCommandsAsync(context, provider);
            foreach (var command in executableCommands.Take(MaxFieldCount))
            {
                var summary = string.IsNullOrEmpty(command.Summary) ? "---" : command.Summary;
                AddField($"`{command.GetCommandFormat(prefix)}`", summary);
            }

            return this;
        }
    }
}
