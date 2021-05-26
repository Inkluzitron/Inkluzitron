using Inkluzitron.Contracts;
using System.Collections.Generic;
using System.Globalization;

namespace Inkluzitron.Modules.Points
{
    public class PointsEmbedMetadata : IEmbedMetadata
    {
        public int Start { get; set; }
        public string EmbedKind => "PointsBoardEmbed";

        public void SaveInto(IDictionary<string, string> destination)
        {
            destination[nameof(Start)] = Start.ToString();
        }

        public bool TryLoadFrom(IReadOnlyDictionary<string, string> values)
        {
            if (!values.TryGetValue(nameof(Start), out var startStr))
                return false;

            if (!int.TryParse(startStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var start))
                return false;

            Start = start;
            return true;
        }
    }
}
