using System.Linq;
using System.Threading.Tasks;
using Discord.Net;
using Discord;
using Discord.WebSocket;
using Inkluzitron.Data;
using Inkluzitron.Extensions;
using Inkluzitron.Models.Settings;
using Microsoft.Extensions.Configuration;

namespace Inkluzitron.Handlers
{
    public class NewbieHandler: IHandler
    {
        private IConfiguration Config { get; }
        private DiscordSocketClient DiscordSocketClient { get; }
        private DatabaseFactory DatabaseFactory { get; }
        private BotSettings BotSettings { get; }

        public NewbieHandler(DiscordSocketClient discordSocketClient,
            DatabaseFactory databaseFactory,
            BotSettings botSettings,
            IConfiguration config)
        {
            DiscordSocketClient = discordSocketClient;
            DatabaseFactory = databaseFactory;
            BotSettings = botSettings;
            Config = config;

            DiscordSocketClient.UserJoined += OnUserJoinedAsync;
            DiscordSocketClient.GuildMemberUpdated += GuildMemberUpdated;
        }

        /// <summary>
        /// Listen for user updates to keep internal record of newbies up to date
        /// </summary>
        /// <param name="userOld">old user data</param>
        /// <param name="userNew">new user data</param>
        /// <returns></returns>
        private async Task GuildMemberUpdated(SocketGuildUser userOld, SocketGuildUser userNew)
        {
            var isOldUserNewbie = userOld.Roles.Any(role => role.Id == BotSettings.NewbieRoleId);
            var isNewUserNewbie = userNew.Roles.Any(role => role.Id == BotSettings.NewbieRoleId);

            if (isOldUserNewbie == isNewUserNewbie)
            {
                return;
            }

            await using var dbContext = DatabaseFactory.Create();
            var userDb = await dbContext.GetOrCreateUserEntityAsync(userNew);

            userDb.IsNewbie = isNewUserNewbie;
            await dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Add newbie role to a newly joined user if the user joined the server for the first time
        /// </summary>
        /// <param name="user">newly joined user</param>
        private async Task OnUserJoinedAsync(SocketGuildUser user)
        {
            await using var dbContext = DatabaseFactory.Create();
            var userDb = await dbContext.GetOrCreateUserEntityAsync(user);

            if (!userDb.IsNewbie)
            {
                return;
            }

            await user.AddRoleAsync(BotSettings.NewbieRoleId);

            try
            {
                await user.SendMessageAsync(Config.GetValue<string>("Welcoming:DirectMessage"));
            }
            catch (HttpException ex) when (ex.DiscordCode == 50007)
            {
                // User has disabled DMs
            }
        }
    }
}
