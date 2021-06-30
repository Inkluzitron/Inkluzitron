namespace Inkluzitron.Modules.Vote
{
    public class EndOfVotingScheduledTaskData
    {
        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
    }
}
