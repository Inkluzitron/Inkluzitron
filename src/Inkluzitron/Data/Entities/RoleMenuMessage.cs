using System.Collections.Generic;

namespace Inkluzitron.Data.Entities
{
    public class RoleMenuMessage
    {
        public string Title { get; set; }
        public bool CanSelectMultiple { get; set; }
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
        public List<RoleMenuMessageRole> Roles { get; set; }
    }
}
