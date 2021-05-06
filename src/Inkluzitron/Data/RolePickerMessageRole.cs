namespace Inkluzitron.Data
{
    public class RolePickerMessageRole
    {
        public RolePickerMessage Message { get; set; }

        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
        public ulong Id { get; set; }
        public string Mention { get; set; }
        public string Emote { get; set; }
        public string Description { get; set; }
    }
}
