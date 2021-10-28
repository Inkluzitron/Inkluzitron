using Inkluzitron.Enums;
using System;
using System.Collections.Generic;
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

        public string Pronouns { get; set; }
        public Gender Gender { get; set; } = Gender.Unspecified;

        public List<DailyUserActivity> DailyActivity { get; set; } = new();

        public ICollection<Badge> Badges { get; } = new List<Badge>();

        public DateTime? LastMessagePointsIncrement { get; set; }
        public DateTime? LastReactionPointsIncrement { get; set; }

        [Timestamp]
        public byte[] Timestamp { get; set; }

        public string KisNickname { get; set; }
        public DateTime? KisLastCheck { get; set; }

        public const int UnsetBirthdayYear = 1800;
        public DateTime? BirthdayDate { get; set; }

        public CommandConsent CommandConsents { get; set; } = CommandConsent.None;
        public bool HasGivenConsentTo(CommandConsent consentKind)
            => (CommandConsents & consentKind) == consentKind;
    }
}
