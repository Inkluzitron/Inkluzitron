using Newtonsoft.Json;

namespace Inkluzitron.Models.BdsmTestOrgApi
{
    public class ResultItem
    {

        [JsonProperty("id", Required = Required.Always)]
        public int Id { get; set; }

        [JsonProperty("score", Required = Required.Always)]
        public int Score { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description{ get; set; }

        [JsonProperty("pairdesc")]
        public string PairDescription { get; set; }

    }
}
