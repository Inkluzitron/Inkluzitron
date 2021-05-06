using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using Inkluzitron.Contracts;
using Inkluzitron.Data;
using Inkluzitron.Enums;
using Microsoft.EntityFrameworkCore;

namespace Inkluzitron.Modules.UserRolePicker
{
    public class RolePickerReactionHandler : IReactionHandler
    {
        private DiscordSocketClient Client { get; }
        private BotDatabaseContext DbContext { get; }

        public RolePickerReactionHandler(DiscordSocketClient client, BotDatabaseContext dbContext)
        {
            Client = client;
            DbContext = dbContext;
        }

        public async Task<bool> HandleReactionChangedAsync(IUserMessage msg, IEmote reaction, IUser user, ReactionEvent eventType)
        {
            // Basic check to optimize number of queries to db
            if (!msg.Content.StartsWith("**\n"))
            {
                return false;
            }

            var channel = msg.Channel as ITextChannel;

            var data = await DbContext.UserRoleMessageItem.AsQueryable()
                .Where(i =>
                    i.GuildId == channel.GuildId &&
                    i.ChannelId == channel.Id &&
                    i.MessageId == msg.Id)
                .ToArrayAsync();

            // check if message is role picker message
            if (data.Length == 0) return false;

            var role = data.FirstOrDefault(d => d.Emote == reaction.ToString());

            // Invalid reaction
            if (role == null)
            {
                await msg.RemoveReactionAsync(reaction, user);
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

            var guildUser = user as IGuildUser;


            if (guildUser == null)
            {
                await msg.RemoveReactionAsync(reaction, user);
                return true;
            }

            if (eventType == ReactionEvent.Removed)
            {
                await guildUser.RemoveRoleAsync(selectedRole);
                return true;
            }

            // Remove roles when in exclusive mode
            if (DbContext.UserRoleMessage.Any(m =>
                m.GuildId == channel.GuildId &&
                m.ChannelId == channel.Id &&
                m.MessageId == msg.Id &&
                m.CanSelectMultiple == false))
            {

                var removeReactions = msg.Reactions
                    .Where(kv => kv.Value.IsMe && kv.Key.ToString() != reaction.ToString())
                    .Select(kv => kv.Key)
                    .ToArray();

                await msg.RemoveReactionsAsync(user, removeReactions);

                var removeRoles = guildRoles.Where(r => guildUser.RoleIds.Contains(r.Id));
                await guildUser.RemoveRolesAsync(removeRoles);
            }

            await guildUser.AddRoleAsync(selectedRole);

            return true;
        }
    }
}
