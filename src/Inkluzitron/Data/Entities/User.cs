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

        public Gender Gender { get; set; } = Gender.Unspecified;

        public List<DailyUserActivity> DailyActivity { get; set; } = new();

        public DateTime? LastMessagePointsIncrement { get; set; }
        public DateTime? LastReactionPointsIncrement { get; set; }

        [Timestamp]
        public byte[] Timestamp { get; set; }

        public CommandConsent CommandConsents { get; set; } = CommandConsent.None;

        public bool HasGivenConsentTo(CommandConsent consentKind)
            => (CommandConsents & consentKind) == consentKind;
    }
}
