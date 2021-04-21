using Discord.WebSocket;
using System.Threading.Tasks;

namespace Inkluzitron.Extensions
{
    static public class SocketGuildExtensions
    {
        static public async Task<SocketGuildUser> GetUserAsync(this SocketGuild guild, ulong id)
        {
            var user = guild.GetUser(id);

            if (user == null)
            {
                await guild.DownloadUsersAsync();
                user = guild.GetUser(id);
            }

            return user;
        }
    }
}
