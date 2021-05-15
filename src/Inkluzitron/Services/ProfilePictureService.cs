using Discord;
using Discord.WebSocket;
using Inkluzitron.Extensions;
using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using SysDrawImage = System.Drawing.Image;

namespace Inkluzitron.Services
{
    public class ProfilePictureService
    {
        private readonly DiscordSocketClient _client;

        public ProfilePictureService(DiscordSocketClient client)
        {
            _client = client;
        }

        public async Task<SysDrawImage> GetProfilePictureAsync(ulong userId, ushort discordSize = 128, Size? size = null)
        {
            var user = await _client.Rest.GetUserAsync(userId);
            return await GetProfilePictureAsync(user, discordSize, size);
        }

        public async Task<SysDrawImage> GetProfilePictureAsync(IUser user, ushort discordSize = 128, Size? size = null)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            if (size == null)
                size = new Size(100, 100);

            var profilePictureData = await user.DownloadProfilePictureAsync(size: discordSize);
            using var memStream = new MemoryStream(profilePictureData);
            using var rawProfileImage = SysDrawImage.FromStream(memStream);
            using var roundedProfileImage = rawProfileImage.RoundImage();

            return roundedProfileImage.ResizeImage(size.Value.Width, size.Value.Height);
        }
    }
}
