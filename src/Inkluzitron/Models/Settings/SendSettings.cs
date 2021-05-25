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
        }

        public string ErrorGuildNotFound { get; }
        public string ErrorRoomNotFound { get; }
        public string ErrorNoContent { get; }
    }
}
