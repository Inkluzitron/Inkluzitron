using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inkluzitron.Data.Entities
{
    public abstract class QuizResultBase
    {
        [Key]
        public long Id { get; set; }

        [Required]
        public DateTime SubmittedAt { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }
        public ulong UserId { get; set; }
    }
}
