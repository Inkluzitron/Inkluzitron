using Inkluzitron.Contracts;
using System.Collections.Generic;
using System.Globalization;

namespace Inkluzitron.Modules.Reminders
{
    public class HelpPageEmbedMetadata : IEmbedMetadata
    {
        public int PageNumber { get; set; }
        public int PageCount { get; set; }
        public string EmbedKind => "HelpPageEmbed";

        public void SaveInto(IDictionary<string, string> destination)
        {
            destination[nameof(PageNumber)] = PageNumber.ToString();
            destination[nameof(PageCount)] = PageCount.ToString();
        }

        public bool TryLoadFrom(IReadOnlyDictionary<string, string> values)
        {
            if (!values.TryGetValue(nameof(PageNumber), out var pageNumberText))
                return false;

            if (!int.TryParse(pageNumberText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pageNumber))
                return false;

            if (!values.TryGetValue(nameof(PageCount), out var pageCountText))
                return false;

            if (!int.TryParse(pageCountText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pageCount))
                return false;

            PageNumber = pageNumber;
            PageCount = pageCount;
            return true;
        }
    }
}
