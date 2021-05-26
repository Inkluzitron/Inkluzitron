using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inkluzitron.Data.Entities
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ulong Id { get; set; }

        [Required]
        public long Points { get; set; } = 0;

        public DateTime? LastMessagePointsIncrement { get; set; }
        public DateTime? LastReactionPointsIncrement { get; set; }

        [Timestamp]
        public byte[] Timestamp { get; set; }
    }
}
