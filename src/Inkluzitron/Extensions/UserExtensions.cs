using Discord;

namespace Inkluzitron.Extensions
{
    static public class UserExtensions
    {
        static public string GetUserOrDefaultAvatarUrl(this IUser user, ImageFormat imageFormat = ImageFormat.Auto, ushort size = 128)
        {
            return user.GetAvatarUrl(imageFormat, size) ?? user.GetDefaultAvatarUrl();
        }

        static public string GetDisplayName(this IUser user)
        {
            if (user is IGuildUser sgu && !string.IsNullOrEmpty(sgu.Nickname))
                return sgu.Nickname;

            return $"{user.Username}#{user.Discriminator}";
        }
    }
}
