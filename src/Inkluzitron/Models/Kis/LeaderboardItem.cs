using Newtonsoft.Json;

namespace Inkluzitron.Models.Kis
{
    public class LeaderboardItem
    {
        [JsonProperty("nickname", Required = Required.Always)]
        public string Nickname { get; set; }

        [JsonProperty("prestige_gain", Required = Required.Always)]
        public double PrestigeGain { get; set; }
    }
}
