using Discord;
using Discord.WebSocket;
using System.Net;
using System.Threading.Tasks;

namespace Inkluzitron.Extensions
{
    static public class UserExtensions
    {
        static public string GetUserAvatar(this IUser user, ImageFormat imageFormat = ImageFormat.Auto, ushort size = 128)
        {
            return user.GetAvatarUrl(imageFormat, size) ?? user.GetDefaultAvatarUrl();
        }

        static public async Task<byte[]> DownloadProfilePictureAsync(this IUser user, ImageFormat imageFormat = ImageFormat.Auto, ushort size = 128)
        {
            var avatarUrl = user.GetAvatarUrl(imageFormat, size) ?? user.GetDefaultAvatarUrl();

            using var client = new WebClient();
            return await client.DownloadDataTaskAsync(avatarUrl);
        }

        static public string GetDisplayName(this IUser user)
        {
            if (user is SocketGuildUser sgu && !string.IsNullOrEmpty(sgu.Nickname))
                return sgu.Nickname;

            return $"{user.Username}#{user.Discriminator}";
        }
    }
}
