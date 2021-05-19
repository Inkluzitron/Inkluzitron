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
        public async Task HelpAsync()
        {
            var availableModules = await CommandService.Modules
                .FindAllAsync(async mod => mod.HasStandaloneHelpPage() && (await mod.GetExecutableCommandsAsync(Context, Provider)).Count > 0);

            if (availableModules.Count == 0)
            {
                await ReplyAsync("Je mi to líto, ale nemáš k dispozici žádné příkazy.");
                return;
            }

            var prefix = Configuration["Prefix"];
            var embed = await new HelpPageEmbed().WithModuleAsync(availableModules.FirstOrDefault(), Context, Provider, availableModules.Count, prefix);
            var message = await ReplyAsync(embed: embed.Build());
            await message.AddReactionsAsync(ReactionSettings.PaginationReactions);
        }
    }
}
