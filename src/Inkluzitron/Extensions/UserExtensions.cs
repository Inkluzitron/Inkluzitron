using Discord;

namespace Inkluzitron.Extensions
{
    static public class UserExtensions
    {
        static public string GetUserOrDefaultAvatarUrl(this IUser user, ImageFormat imageFormat = ImageFormat.Auto, ushort size = 128)
        {
            return user.GetAvatarUrl(imageFormat, size) ?? user.GetDefaultAvatarUrl();
        }
    }
}
