using Inkluzitron.Contracts;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Inkluzitron.Modules.Reminders
{
    public class ReminderEmbedMetadata : IEmbedMetadata
    {
        public ulong UserId { get; set; }
        public long ReminderId { get; set; }
        public DateTimeOffset When { get; set; }

        public string EmbedKind => "ReminderEmbed";

        public void SaveInto(IDictionary<string, string> destination)
        {
            destination[nameof(UserId)] = UserId.ToString();
            destination[nameof(ReminderId)] = ReminderId.ToString();
            destination[nameof(When)] = When.ToString("O");
        }

        public bool TryLoadFrom(IReadOnlyDictionary<string, string> values)
        {
            if (!values.TryGetValue(nameof(UserId), out var userIdText))
                return false;

            if (!ulong.TryParse(userIdText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var userId))
                return false;

            if (!values.TryGetValue(nameof(ReminderId), out var reminderIdText))
                return false;

            if (!long.TryParse(reminderIdText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var reminderId))
                return false;

            if (!values.TryGetValue(nameof(When), out var whenText))
                return false;

            if (!DateTimeOffset.TryParseExact(whenText, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out var when))
                return false;

            UserId = userId;
            ReminderId = reminderId;
            When = when;
            return true;
        }
    }
}
