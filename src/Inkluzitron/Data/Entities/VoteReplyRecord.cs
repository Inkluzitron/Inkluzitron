using System;

namespace Inkluzitron.Data.Entities
{
    public class VoteReplyRecord
    {
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
        public ulong ReplyId { get; set; }
        public DateTime RecordCreatedAt { get; set; } = DateTime.UtcNow;
    }
}
