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
        public async Task<HelpPageEmbed> WithModuleAsync(ModuleInfo module, ICommandContext context, IServiceProvider provider, int pagesCount, string prefix,
            int page = 1)
        {
            var bot = context.Client.CurrentUser;

            this.WithTitle(module.Name);
            this.WithCurrentTimestamp();
            this.WithAuthor(new EmbedAuthorBuilder()
            {
                Name = "Nápověda",
                IconUrl = bot.GetAvatarUrl()
            });
            this.WithFooter($"{page}/{pagesCount}");
            this.WithMetadata(new HelpPageEmbedMetadata { PageNumber = page, PageCount = pagesCount });

            if (!string.IsNullOrEmpty(module.Summary))
                this.WithDescription(module.Summary);

            var executableCommands = await module.GetExecutableCommandsAsync(context, provider);
            foreach (var command in executableCommands.Take(MaxFieldCount))
            {
                var summary = string.IsNullOrEmpty(command.Summary) ? "*Tento příkaz nemá popis.*" : command.Summary;
                this.AddField($"{command.GetCommandFormat(prefix)}", summary);
            }

            return this;
        }
    }
}
