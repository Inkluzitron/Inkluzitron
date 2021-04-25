﻿using Discord;
using System.Net;
using System.Threading.Tasks;

namespace Inkluzitron.Extensions
{
    static public class UserExtensions
    {
        static public async Task<byte[]> DownloadProfilePictureAsync(this IUser user, ImageFormat imageFormat = ImageFormat.Auto, ushort size = 128)
        {
            var avatarUrl = user.GetAvatarUrl(imageFormat, size) ?? user.GetDefaultAvatarUrl();

            using var client = new WebClient();
            return await client.DownloadDataTaskAsync(avatarUrl);
        }
    }
}