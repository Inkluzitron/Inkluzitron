using Newtonsoft.Json;

namespace Inkluzitron.Models.BdsmTestOrgApi
{
    public class ResultItem
    {

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("score")]
        public int Score { get; set; }
    }
}
