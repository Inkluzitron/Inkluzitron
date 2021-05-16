using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Inkluzitron.Models.BdsmTestOrgApi
{
    public class Result
    {
        [JsonConverter(typeof(Newtonsoft.Json.Converters.IsoDateTimeConverter))]
        [JsonProperty("date")]
        public DateTime Date { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("gender")]
        public string Gender { get; set; }

        [JsonProperty("auth")]
        public bool RegisteredUser { get; set; }

        [JsonProperty("scores")]
        public List<ResultItem> Traits { get; set; }
    }
}
