using Newtonsoft.Json;

namespace Inkluzitron.Models.KisApi
{
    public class KisLeaderboardItem
    {
        [JsonProperty("nickname", Required = Required.Always)]
        public string Nickname { get; set; }

        [JsonProperty("prestige_gain", Required = Required.Always)]
        public int PrestigeGain { get; set; }
    }
}
