using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Inkluzitron.Models
{
    public class UrbanQueryResult
    {
        public string Query { get; set; }

        [JsonProperty("list", Required = Required.Always)]
        public List<UrbanDefinition> Definitions { get; set; }
    }

    public class UrbanDefinition
    {
        [JsonProperty("defid", Required = Required.Always)]
        public int Id { get; set; }

        [JsonProperty("word", Required = Required.Always)]
        public string Word { get; set; }

        [JsonProperty("author")]
        public string Author { get; set; }

        [JsonProperty("definition", Required = Required.Always)]
        public string Definition { get; set; }

        [JsonProperty("permalink", Required = Required.Always)]
        public string Permalink { get; set; }

        [JsonProperty("thumbs_up")]
        public int ThumbsUp { get; set; }

        [JsonProperty("thumbs_down")]
        public int ThumbsDown { get; set; }

        [JsonConverter(typeof(Newtonsoft.Json.Converters.IsoDateTimeConverter))]
        [JsonProperty("written_on")]
        public DateTime WrittenOn { get; set; }

        [JsonProperty("example")]
        public string ExampleUsage { get; set; }
    }
}
