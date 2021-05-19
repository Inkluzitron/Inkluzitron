using Inkluzitron.Contracts;
using System.Collections.Generic;
using System.Globalization;

namespace Inkluzitron.Modules.Urban
{
    public class UrbanEmbedMetadata : IEmbedMetadata
    {
        public int PageNumber { get; set; }
        public string SearchQuery { get; set; }
        public string EmbedKind => "UrbanEmbed";

        public void SaveInto(IDictionary<string, string> destination)
        {
            destination["pagenum"] = PageNumber.ToString();
            destination["query"] = SearchQuery;
        }

        public bool TryLoadFrom(IReadOnlyDictionary<string, string> values)
        {
            if (!values.TryGetValue("pagenum", out var pageNumberText))
                return false;

            if (!int.TryParse(pageNumberText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var pageNumber))
                return false;

            if (!values.TryGetValue("query", out var searchQuery))
                return false;

            PageNumber = pageNumber;
            SearchQuery = searchQuery;
            return true;
        }
    }
}
