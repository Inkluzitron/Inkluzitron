using Discord;
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

        /// <summary>
        /// Calculates maximum file size in attachment.
        /// </summary>
        static public int CalculateFileUploadLimit(this SocketGuild guild)
        {
            return (guild.PremiumTier switch
            {
                PremiumTier.Tier2 => 50,
                PremiumTier.Tier3 => 100,
                _ => 8
            }) * 1024 * 1024;
        }
    }
}
