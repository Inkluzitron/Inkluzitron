using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Inkluzitron.Contracts;
using Inkluzitron.Extensions;
using Inkluzitron.Models.Settings;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Inkluzitron.Modules.Help
{
    public class HelpReactionHandler : IReactionHandler
    {
        private ReactionSettings ReactionSettings { get; }
        private CommandService CommandService { get; }
        private DiscordSocketClient Client { get; }
        private IServiceProvider Provider { get; }
        private IConfiguration Configuration { get; }

        public HelpReactionHandler(ReactionSettings reactionSettings, CommandService commandService, DiscordSocketClient client,
            IServiceProvider provider, IConfiguration configuration)
        {
            ReactionSettings = reactionSettings;
            CommandService = commandService;
            Client = client;
            Provider = provider;
            Configuration = configuration;
        }

        public async Task<bool> HandleReactionAddedAsync(IUserMessage message, IEmote reaction, IUser user)
        {
            var embed = message.Embeds.FirstOrDefault();
            if (embed == null || embed.Author == null || embed.Footer == null)
                return false; // Embed checks

            if (!ReactionSettings.PaginationReactions.Contains(reaction))
                return false; // Reaction check.

            if (embed.Author.Value.Name != user.ToString() || message.ReferencedMessage == null)
                return false;

            if (!embed.TryParseMetadata<HelpPageEmbedMetadata>(out var metadata))
                return false; // Not a help embed.

            var context = new CommandContext(Client, message.ReferencedMessage);
            var availableModules = await CommandService.Modules
                .FindAllAsync(async mod => (await mod.GetExecutableCommandsAsync(context, Provider)).Count > 0);

            int maxPages = Math.Min(metadata.PageCount, availableModules.Count); // Maximal count of available pages.
            int newPage = metadata.PageNumber;
            if (reaction.Equals(ReactionSettings.MoveToFirst))
                newPage = 1;
            else if (reaction.Equals(ReactionSettings.MoveToLast))
                newPage = maxPages;
            else if (reaction.Equals(ReactionSettings.MoveToNext) && newPage < maxPages)
                newPage++;
            else if (reaction.Equals(ReactionSettings.MoveToPrevious) && newPage > 1)
                newPage--;

            if (newPage != metadata.PageNumber)
            {
                var module = availableModules[newPage - 1];
                var newEmbed = (await new HelpPageEmbed()
                    .WithModuleAsync(module, user, context, Provider, maxPages, Configuration["Prefix"], newPage))
                    .Build();

                await message.ModifyAsync(msg => msg.Embed = newEmbed);
            }

            await message.RemoveReactionAsync(reaction, user);
            return true;
        }
    }
}
