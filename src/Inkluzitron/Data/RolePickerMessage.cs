using System.Collections.Generic;

namespace Inkluzitron.Data
{
    public class RolePickerMessage
    {
        public string Title { get; set; }
        public bool CanSelectMultiple { get; set; }
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
        public List<RolePickerMessageRole> Roles { get; set; }
    }
}
