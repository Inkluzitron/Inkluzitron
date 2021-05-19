using Newtonsoft.Json;
using System.Collections.Generic;

namespace Inkluzitron.Models.UrbanApi
{
    public class UrbanQueryResult
    {
        public string Query { get; set; }

        [JsonProperty("list", Required = Required.Always)]
        public List<UrbanDefinition> Definitions { get; set; }
    }
}
