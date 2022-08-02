using Inkluzitron.Extensions;
using Microsoft.Extensions.Configuration;

namespace Inkluzitron.Models.Settings
{
    public class SendSettings
    {
        public SendSettings(IConfiguration config)
        {
            var section = config.GetSection("Send");
            section.AssertExists();

            ErrorGuildNotFound = section.GetRequired<string>(nameof(ErrorGuildNotFound));
            ErrorRoomNotFound = section.GetRequired<string>(nameof(ErrorRoomNotFound));
            ErrorNoContent = section.GetRequired<string>(nameof(ErrorNoContent));
            ErrorRoomNotWhitelisted = section.GetRequired<string>(nameof(ErrorRoomNotWhitelisted));
            ListMessage = section.GetRequired<string>(nameof(ListMessage));
            RoomWhitelist = section.GetRequired<ulong[]>(nameof(RoomWhitelist));
            WhitelistEnabled = section.GetRequired<bool>(nameof(WhitelistEnabled));
        }

        public string ErrorGuildNotFound { get; }
        public string ErrorRoomNotFound { get; }
        public string ErrorNoContent { get; }
        public string ErrorRoomNotWhitelisted { get; }
        public string ListMessage { get; }
        public ulong[] RoomWhitelist { get; }
        public bool WhitelistEnabled { get; }
    }
}
