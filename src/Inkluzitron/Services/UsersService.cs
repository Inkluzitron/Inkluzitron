using Discord;
using Discord.WebSocket;
using Inkluzitron.Extensions;
using Inkluzitron.Models.Settings;
using System.Threading.Tasks;

namespace Inkluzitron.Services
{
    public class UsersService
    {
        private BotSettings BotSettings { get; }
        private DiscordSocketClient DiscordClient { get; }

        public UsersService(BotSettings botSettings, DiscordSocketClient discordClient)
        {
            BotSettings = botSettings;
            DiscordClient = discordClient;
        }

        public async Task<SocketGuildUser> GetUserFromHomeGuild(ulong userId)
        {
            var homeGuild = DiscordClient.GetGuild(BotSettings.HomeGuildId);

            return await homeGuild.GetUserAsync(userId);
        }

        public Task<SocketGuildUser> GetUserFromHomeGuild(IUser user)
            => GetUserFromHomeGuild(user.Id);

        public async Task<string> GetDisplayNameAsync(ulong userId)
        {
            var homeGuild = DiscordClient.GetGuild(BotSettings.HomeGuildId);

            var user = await homeGuild.GetUserAsync(userId);

            if (user == null)
                return null;

            return user.Nickname ?? user.Username;
        }

        public async Task<string> GetDisplayNameAsync(IUser user)
        {
            if (user is IGuildUser sgu && !string.IsNullOrEmpty(sgu.Nickname))
                return sgu.Nickname;

            var guildUserName = await GetDisplayNameAsync(user.Id);

            return guildUserName ?? user.Username;
        }
    }
}
