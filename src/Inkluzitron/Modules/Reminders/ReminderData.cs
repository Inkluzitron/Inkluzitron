using Newtonsoft.Json;

namespace Inkluzitron.Modules.Reminders
{
    public class ReminderData
    {
        [JsonProperty(PropertyName = "userId")]
        public ulong UserId { get; set; }

        [JsonProperty(PropertyName = "messageUrl", Required = Required.Always)]
        public string MessageUrl { get; set; }

        [JsonProperty(PropertyName = "reason", Required = Required.Always)]
        public string Reason { get; set; }
    }
}
