using Discord;
using Discord.Commands;
using Inkluzitron.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Inkluzitron.Modules.Reminders
{
    public class HelpPageEmbed : EmbedBuilder
    {
        public async Task<HelpPageEmbed> WithModuleAsync(ModuleInfo module, ICommandContext context, IServiceProvider provider, int pagesCount, string prefix,
            int page = 1)
        {
            var bot = context.Client.CurrentUser;

            WithTitle(module.Name);
            WithColor(new Color(241, 190, 223));
            WithCurrentTimestamp();
            WithAuthor(new EmbedAuthorBuilder()
            {
                Name = "Nápověda",
                IconUrl = bot.GetAvatarUrl()
            });
            WithFooter($"{page}/{pagesCount}");
            this.WithMetadata(new HelpPageEmbedMetadata { PageNumber = page, PageCount = pagesCount });

            if (!string.IsNullOrEmpty(module.Summary))
                WithDescription(module.Summary);

            var executableCommands = await module.GetExecutableCommandsAsync(context, provider);
            foreach (var command in executableCommands.Take(MaxFieldCount))
            {
                var summary = string.IsNullOrEmpty(command.Summary) ? "*Tento příkaz nemá popis.*" : command.Summary;

                var aliases = command.GetAliasesFormat(prefix);
                if (!string.IsNullOrEmpty(aliases))
                    aliases = $"**Alias:** *{aliases}*\n";

                AddField($"{command.GetCommandFormat(prefix)}", $"{aliases}{summary}");
            }

            return this;
        }
    }
}
