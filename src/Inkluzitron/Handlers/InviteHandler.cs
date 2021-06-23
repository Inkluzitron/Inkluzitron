using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using Inkluzitron.Data;
using Inkluzitron.Extensions;
using Inkluzitron.Services;
using Microsoft.EntityFrameworkCore;

namespace Inkluzitron.Handlers
{
    /// <summary>
    /// Handler to catch event on joining new user
    /// </summary>
    public class InviteHandler : IHandler
    {
        private DiscordSocketClient DiscordSocketClient { get; }

        private DatabaseFactory DatabaseFactory { get; }
        private UsersService UsersService { get; }

        public InviteHandler(DiscordSocketClient discordSocketClient,
            DatabaseFactory databaseFactory,
            UsersService usersService)
        {
            DiscordSocketClient = discordSocketClient;
            DatabaseFactory = databaseFactory;
            UsersService = usersService;

            DiscordSocketClient.UserJoined += OnUserJoinedAsync;
        }

        /// <summary>
        /// Checks which invite link has been used by newly joined user and
        /// updates corresponding record in database for it
        /// </summary>
        /// <param name="user">newly joined user</param>
        private async Task OnUserJoinedAsync(SocketGuildUser user)
        {
            await using var dbContext = DatabaseFactory.Create();
            var inviteLinks = await user.Guild.GetInvitesAsync();
            var inviteeDb = await dbContext.GetOrCreateUserEntityAsync(user);

            foreach (var link in inviteLinks)
            {
                if (link.Inviter.Id != DiscordSocketClient.CurrentUser.Id) continue;
                if (link.Uses == 0) continue;

                var invite = await dbContext.Invites.AsQueryable()
                    .Where(i => i.InviteLink == link.Url)
                    .FirstOrDefaultAsync();

                if (invite == null) continue;

                dbContext.Invites.Update(invite).Entity.UsedByUserId = inviteeDb.Id;
                await dbContext.SaveChangesAsync();

                await link.DeleteAsync();

                await user.Guild.DefaultChannel.SendMessageAsync(
                    "Vítej ***" +
                    user.Username +
                    "*** na našem serveru!");
                return;
            }

            await user.Guild.DefaultChannel.SendMessageAsync(
                "Nový uživatel " +
                user.Username +
                " se připojil na server pomocí odkazu, který nebyl vytvořen `$invite` příkazem!");
        }
    }
}
