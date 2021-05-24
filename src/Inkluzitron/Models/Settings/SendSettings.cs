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

            GuildId = section.GetRequired<ulong>(nameof(GuildId));
            ErrorDmOnly = section.GetRequired<string>(nameof(ErrorDmOnly));
            ErrorGuildNotFound = section.GetRequired<string>(nameof(ErrorGuildNotFound));
            ErrorRoomNotFound = section.GetRequired<string>(nameof(ErrorRoomNotFound));
            ErrorNoContent = section.GetRequired<string>(nameof(ErrorNoContent));
            ConfirmationMessage = section.GetRequired<string>(nameof(ConfirmationMessage));
        }

        public ulong GuildId { get; }
        public string ErrorDmOnly { get; }
        public string ErrorGuildNotFound { get; }
        public string ErrorRoomNotFound { get; }
        public string ErrorNoContent { get; }
        public string ConfirmationMessage { get; }
    }
}
