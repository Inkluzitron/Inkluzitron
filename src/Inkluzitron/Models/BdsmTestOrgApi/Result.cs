using Inkluzitron.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace Inkluzitron.Models.BdsmTestOrgApi
{
    public class Result
    {
        [JsonConverter(typeof(Newtonsoft.Json.Converters.IsoDateTimeConverter))]
        [JsonProperty("date", Required = Required.Always)]
        public DateTime Date { get; set; }

        [JsonProperty("scores", Required = Required.Always)]
        public List<ResultItem> Traits { get; set; }

        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Gender Gender { get; set; } = Gender.Unspecified;

        [JsonProperty("auth")]
        public bool RegisteredUser { get; set; }

    }
}
