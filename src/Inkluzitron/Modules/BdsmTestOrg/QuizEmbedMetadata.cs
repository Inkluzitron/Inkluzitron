using Inkluzitron.Contracts;
using System.Collections.Generic;
using System.Globalization;

namespace Inkluzitron.Modules.BdsmTestOrg
{
    public class QuizEmbedMetadata : IEmbedMetadata
    {
        public string EmbedKind { get; } = "BdsmQuizEmbed";

        public ulong UserId { get; set; }
        public ulong ResultId { get; set; }
        public int PageNumber { get; set; }

        public void SaveInto(IDictionary<string, string> destination)
        {
            destination["u"] = UserId.ToString("X");
            destination["r"] = ResultId.ToString("X");
            destination["p"] = PageNumber.ToString("X");
        }

        public bool TryLoadFrom(IReadOnlyDictionary<string, string> values)
        {
            ulong userId, resultId;
            int pageNumber;

            userId = resultId = 0;
            pageNumber = 0;

            var success = values.TryGetValue("u", out var userIdHex)
                && values.TryGetValue("r", out var resultIdHex)
                && values.TryGetValue("p", out var pageNumberHex)
                && ulong.TryParse(userIdHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out userId)
                && ulong.TryParse(resultIdHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out resultId)
                && int.TryParse(pageNumberHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out pageNumber);

            if (success)
            {
                UserId = userId;
                ResultId = resultId;
                PageNumber = pageNumber;
                return true;
            }

            return false;
        }
    }
}
