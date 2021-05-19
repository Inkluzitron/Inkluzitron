using Discord;
using Discord.Commands;
using Inkluzitron.Extensions;
using Inkluzitron.Models.Settings;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Inkluzitron.Modules.Help
{
    [Name("Nápověda")]
    public class HelpModule : ModuleBase
    {
        private CommandService CommandService { get; }
        private ReactionSettings ReactionSettings { get; }
        private IServiceProvider Provider { get; }
        private IConfiguration Configuration { get; }

        public HelpModule(CommandService commandService, ReactionSettings reactionSettings, IServiceProvider provider, IConfiguration configuration)
        {
            CommandService = commandService;
            ReactionSettings = reactionSettings;
            Provider = provider;
            Configuration = configuration;
        }

        [Command("help")]
        [Summary("Zobrazí nápovědu.")]
        public Task HelpAsync()
            => SearchHelpAsync(null);

        [Command("help")]
        [Summary("Zobrazí nápovědu pro zadaný příkaz.")]
        public async Task SearchHelpAsync([Remainder][Name("příkaz")]string query)
        {
            var availableModules = await CommandService.Modules
                .FindAllAsync(async mod => (await mod.GetExecutableCommandsAsync(Context, Provider)).Count > 0);

            var module = availableModules.FirstOrDefault();

            if (module == null)
            {
                await ReplyAsync("Je mi to líto, ale nemáš k dispozici žádné příkazy.");
                return;
            }

            if (!string.IsNullOrEmpty(query))
            {
                var found = availableModules.FirstOrDefault(
                    m => m.Commands.Any(
                        c => c.Aliases.Any(
                            a => a.Contains(query))));

                if(found != null)
                    module = found;
            }


            var prefix = Configuration["Prefix"];
            var embed = await new HelpPageEmbed().WithModuleAsync(
                module, Context, Provider, availableModules.Count, prefix, availableModules.IndexOf(module)+1);


            var message = await ReplyAsync(embed: embed.Build());
            await message.AddReactionsAsync(ReactionSettings.PaginationReactions);
        }
    }
}
