using Inkluzitron.Enums;
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

        public string Name { get; set; }

        public Gender Gender { get; set; } = Gender.Unspecified;

        [Required]
        public long Points { get; set; } = 0;

        public DateTime? MutedUntil { get; set; }

        public DateTime? LastMessagePointsIncrement { get; set; }
        public DateTime? LastReactionPointsIncrement { get; set; }

        [Timestamp]
        public byte[] Timestamp { get; set; }
    }
}
