using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inkluzitron.Data.Entities
{
    public class Invite
    {
        [Key]
        public long Id { get; set; }

        public string InviteCode { get; set; }

        public DateTime? GeneratedAt { get; set; }

        [ForeignKey(nameof(GeneratedByUserId))]
        public User GeneratedBy { get; set; }
        public ulong GeneratedByUserId { get; set; }

        [ForeignKey(nameof(UsedByUserId))]
        public User UsedBy { get; set; }
        public ulong? UsedByUserId { get; set; }
    }
}
