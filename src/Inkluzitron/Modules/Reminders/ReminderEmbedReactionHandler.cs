using Discord;
using Inkluzitron.Contracts;
using Inkluzitron.Data.Entities;
using Inkluzitron.Extensions;
using Inkluzitron.Models.Settings;
using Inkluzitron.Modules.Reminders;
using Inkluzitron.Services;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;

namespace Inkluzitron.Modules.Reminders
{
    public class ReminderEmbedReactionHandler : IReactionHandler
    {
        private ReactionSettings ReactionSettings { get; }
        private ReminderManager ReminderManager { get; }
        private IConfiguration Configuration { get; }

        public ReminderEmbedReactionHandler(ReactionSettings reactionSettings, ReminderManager reminderManager, IConfiguration configuration)
        {
            ReactionSettings = reactionSettings;
            ReminderManager = reminderManager;
            Configuration = configuration;
        }

        public async Task<bool> HandleReactionAddedAsync(IUserMessage message, IEmote reaction, IUser user)
        {
            var embed = message.Embeds.FirstOrDefault();
            if (embed == null || embed.Author == null || embed.Footer == null)
                return false; // Embed checks

            if (!ReactionSettings.PaginationReactions.Any(emote => emote.IsEqual(reaction)))
                return false; // Reaction check.

            if (message.ReferencedMessage == null)
                return false;

            if (!embed.TryParseMetadata<ReminderEmbedMetadata>(out var metadata))
                return false; // Not a help embed.

            var anchor = (metadata.ReminderId, metadata.When);

            var embedData = await ReminderManager.FindForUserAsync(
                user.Id,
                findBefore: reaction.IsEqual(ReactionSettings.MoveToPrevious) ? anchor : null,
                findAfter: reaction.IsEqual(ReactionSettings.MoveToNext) ? anchor : null,
                findFirst: reaction.IsEqual(ReactionSettings.MoveToFirst),
                findLast: reaction.IsEqual(ReactionSettings.MoveToLast)
            );

            if (newPage != metadata.PageNumber)
            {
                var module = availableModules[newPage - 1];
                var newEmbed = (await new HelpPageEmbed()
                    .WithModuleAsync(module, context, Provider, maxPages, Configuration["Prefix"], newPage))
                    .Build();

                await message.ModifyAsync(msg => msg.Embed = newEmbed);
            }

            if (!context.IsPrivate) // DMs have blocked removing reactions.
                await message.RemoveReactionAsync(reaction, user);
            return true;
        }
    }
}
