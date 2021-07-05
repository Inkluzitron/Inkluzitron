using Discord;
using System;
using System.Collections.Generic;

namespace Inkluzitron.Models.Vote
{
    public class VoteDefinition
    {
        public string Question { get; set; }
        public Dictionary<IEmote, string> Options { get; set; } = new();
        public DateTimeOffset? Deadline { get; set; }

        public bool IsPastDeadline()
            => Deadline is DateTimeOffset specifiedDeadline && DateTimeOffset.UtcNow >= specifiedDeadline;

        public bool Validate(out string problemDescription)
        {
            if (string.IsNullOrWhiteSpace(Question))
                problemDescription = "Chybí otázka.";
            else if (Options.Count == 0)
                problemDescription = "Chybí odpověď.";
            else
            {
                problemDescription = null;
                return true;
            }

            return false;
        }
    }
}
