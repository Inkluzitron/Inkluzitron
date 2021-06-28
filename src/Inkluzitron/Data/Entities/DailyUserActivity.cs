using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inkluzitron.Data.Entities
{
    public class DailyUserActivity
    {
        [ForeignKey(nameof(UserId))]
        public User User { get; set; }
        public ulong UserId { get; set; }

        [Required]
        public DateTime Day { get; set; } = DateTime.Today;

        [Required]
        public long Points { get; set; } = 0;

        [Required]
        public long MessagesSent { get; set; } = 0;

        [Required]
        public long ReactionsAdded { get; set; } = 0;

        [Timestamp]
        public byte[] Timestamp { get; set; }
    }
}
