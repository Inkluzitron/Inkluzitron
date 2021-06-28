using Inkluzitron.Contracts;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Inkluzitron.Modules.Points
{
    public class PointsEmbedMetadata : IEmbedMetadata
    {
        public int Start { get; set; }
        public DateTime? DateFrom { get; set; }
        public string EmbedKind => "PointsBoardEmbed";

        public void SaveInto(IDictionary<string, string> destination)
        {
            destination[nameof(Start)] = Start.ToString();
            if(DateFrom != null)
                destination[nameof(DateFrom)] = DateFrom.Value.ToUniversalTime().ToString("s");
        }

        public bool TryLoadFrom(IReadOnlyDictionary<string, string> values)
        {
            if (!values.TryGetValue(nameof(Start), out var startStr))
                return false;

            if (!int.TryParse(startStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var start))
                return false;

            if (values.TryGetValue(nameof(DateFrom), out var dateFromStr))
                if (DateTime.TryParseExact(dateFromStr, "s", null, DateTimeStyles.AssumeUniversal, out var dateFrom))
                    DateFrom = dateFrom.ToLocalTime();

            Start = start;
            return true;
        }
    }
}
