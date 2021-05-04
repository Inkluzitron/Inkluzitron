using Discord;
using Discord.Commands;
using Inkluzitron.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Inkluzitron.Modules.Help
{
    public class HelpPageEmbed : EmbedBuilder
    {
        public async Task<HelpPageEmbed> WithModuleAsync(ModuleInfo module, IUser author, ICommandContext context, IServiceProvider provider, int pagesCount, string prefix,
            int page = 1)
        {
            this.WithTitle(module.Name);
            this.WithCurrentTimestamp();
            this.WithAuthor(author);
            this.WithFooter($"Nápověda | {page}/{pagesCount}");
            this.WithMetadata(new HelpPageEmbedMetadata { PageNumber = page, PageCount = pagesCount });

            var executableCommands = await module.GetExecutableCommandsAsync(context, provider);
            foreach (var command in executableCommands.Take(MaxFieldCount))
            {
                var summary = string.IsNullOrEmpty(command.Summary) ? "---" : command.Summary;
                this.AddField($"`{command.GetCommandFormat(prefix)}`", summary);
            }

            return this;
        }
    }
}
