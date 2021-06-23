using Inkluzitron.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Inkluzitron.Data.Entities
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public ulong Id { get; set; }

        public string Name { get; set; }

        public Gender Gender { get; set; } = Gender.Unspecified;

        public List<UserPoints> DailyPoints { get; set; } = new();

        public DateTime? LastMessagePointsIncrement { get; set; }
        public DateTime? LastReactionPointsIncrement { get; set; }

        [Timestamp]
        public byte[] Timestamp { get; set; }

        public CommandConsent CommandConsents { get; set; } = CommandConsent.None;

        public bool HasGivenConsentTo(CommandConsent consentKind)
            => (CommandConsents & consentKind) == consentKind;

        public long GetTotalPoints(DateTime? from = null)
            => DailyPoints.Where(p => !from.HasValue || p.Day >= from.Value).Sum(p => p.Points);

        public void AddPoints(long increment)
        {
            var current = DateTime.Now.Date;

            var currentDayPoints = DailyPoints.FirstOrDefault(p => p.Day == current);

            if(currentDayPoints == null)
            {
                currentDayPoints = new UserPoints();
                DailyPoints.Add(currentDayPoints);
            }

            currentDayPoints.Points += increment;
        }
    }
}
