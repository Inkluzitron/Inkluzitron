using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Inkluzitron.Contracts;
using Inkluzitron.Data;
using Inkluzitron.Enums;
using Microsoft.EntityFrameworkCore;

namespace Inkluzitron.Modules.RoleMenu
{
    public class RoleMenuReactionHandler : IReactionHandler
    {
        private DatabaseFactory DatabaseFactory { get; }

        public RoleMenuReactionHandler(DatabaseFactory databaseFactory)
        {
            DatabaseFactory = databaseFactory;
        }

        public async Task<bool> HandleReactionChangedAsync(IUserMessage message, IEmote reaction, IUser user, ReactionEvent eventType)
        {
            // Basic check to optimize number of queries to db
            if (!message.Content.StartsWith("**\n"))
            {
                return false;
            }

            var channel = message.Channel as ITextChannel;

            using var dbContext = DatabaseFactory.Create();
            var data = await dbContext.UserRoleMessageItem.AsQueryable()
                .Where(i =>
                    i.GuildId == channel.GuildId &&
                    i.ChannelId == channel.Id &&
                    i.MessageId == message.Id)
                .ToArrayAsync();

            // check if message is role picker message
            if (data.Length == 0) return false;

            var role = Array.Find(data, d => d.Emote == reaction.ToString());

            // Invalid reaction
            if (role == null)
            {
                await message.RemoveReactionAsync(reaction, user);
                return true;
            }

            var guildRoles = channel.Guild.Roles.Where(r => data.Any(e => r.Id == e.Id));
            var selectedRole = guildRoles.FirstOrDefault(r => r.Id == role.Id);

            // Role does not exists
            if (selectedRole == null)
            {
                // TODO Remove role from db
                return true;
            }

            if (user is not IGuildUser guildUser)
            {
                await message.RemoveReactionAsync(reaction, user);
                return true;
            }

            if (eventType == ReactionEvent.Removed)
            {
                await guildUser.RemoveRoleAsync(selectedRole);
                return true;
            }

            // Remove roles when in exclusive mode
            if (dbContext.UserRoleMessage.Any(m =>
                m.GuildId == channel.GuildId &&
                m.ChannelId == channel.Id &&
                m.MessageId == message.Id &&
                !m.CanSelectMultiple))
            {
                var removeReactions = message.Reactions
                    .Where(kv => kv.Value.IsMe && kv.Key.ToString() != reaction.ToString())
                    .Select(kv => kv.Key)
                    .ToArray();

                await message.RemoveReactionsAsync(user, removeReactions);

                var removeRoles = guildRoles.Where(r => guildUser.RoleIds.Contains(r.Id));
                await guildUser.RemoveRolesAsync(removeRoles);
            }

            await guildUser.AddRoleAsync(selectedRole);

            return true;
        }
    }
}
