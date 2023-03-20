using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Inkluzitron.Data;
using Inkluzitron.Extensions;
using Inkluzitron.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Inkluzitron.Handlers
{
    /// <summary>
    /// Handler to catch event on joining new user
    /// </summary>
    public class InviteHandler : IHandler
    {
        private IConfiguration Config { get; }
        private DiscordSocketClient DiscordSocketClient { get; }

        private DatabaseFactory DatabaseFactory { get; }
        private UsersService UsersService { get; }

        public InviteHandler(DiscordSocketClient discordSocketClient,
            DatabaseFactory databaseFactory,
            UsersService usersService,
            IConfiguration config)
        {
            DiscordSocketClient = discordSocketClient;
            DatabaseFactory = databaseFactory;
            UsersService = usersService;
            Config = config;

            DiscordSocketClient.UserJoined += OnUserJoinedAsync;
            DiscordSocketClient.UserLeft += OnUserLeftAsync;
        }

        private string GetRandomMessageTemplate(string templateKey)
        {
            var firstLine = Config.GetValue<string[]>(templateKey);

            return firstLine[ThreadSafeRandom.Next(firstLine.Length)];
        }

        private async Task OnUserLeftAsync(SocketGuildUser user)
        {
            await using var dbContext = DatabaseFactory.Create();
            var userDb = await dbContext.GetOrCreateUserEntityAsync(user);

            var leaveMessageTemplate = GetRandomMessageTemplate("Welcoming:LeaveMessage");

            var leaveMessage = string.Format(
                leaveMessageTemplate,
                userDb.Gender,
                Format.Sanitize(await UsersService.GetDisplayNameAsync(user))
            );

            await user.Guild.DefaultChannel.SendMessageAsync(leaveMessage);
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

                var code = link.Url.Split('/').Last();

                var invite = await dbContext.Invites.AsQueryable()
                    .Where(i => i.InviteCode == code)
                    .FirstOrDefaultAsync();

                if (invite == null) continue;

                invite.UsedByUserId = inviteeDb.Id;
                await dbContext.SaveChangesAsync();

                await link.DeleteAsync();

                var welcomeMessageTemplate = GetRandomMessageTemplate(inviteeDb.IsNewbie ? "Welcoming:JoinFirstTimeMessage" : "Welcoming:JoinMessage");

                var welcomeMessage = string.Format(
                    welcomeMessageTemplate,
                    inviteeDb.Gender,
                    Format.Sanitize(await UsersService.GetDisplayNameAsync(user)),
                    Format.Sanitize(await UsersService.GetDisplayNameAsync(invite.GeneratedByUserId))
                );

                await user.Guild.DefaultChannel.SendMessageAsync(welcomeMessage);
                return;
            }

            await user.Guild.DefaultChannel.SendMessageAsync(
                $"Uživatel **{Format.Sanitize(user.Username)}** se připojil na server pomocí odkazu, který nebyl vytvořen `$invite` příkazem!");
        }
    }
}
