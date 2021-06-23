using Discord;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inkluzitron.Data.Entities
{
    public class DailyUserActivity
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public DateTime Day { get; set; } = DateTime.Now.Date;

        [Required]
        public long Points { get; set; } = 0;

        [Required]
        public long MessagesSent { get; set; } = 0;

        [Required]
        public long ReactionsAdded { get; set; } = 0;

        [ForeignKey(nameof(UserId))]
        public User User { get; set; }
        public ulong UserId { get; set; }
    }
}
