namespace Inkluzitron.Models.Vote
{
    public class EndOfVotingScheduledTask
    {
        public const string Identifier = nameof(EndOfVotingScheduledTask);

        public ulong GuildId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
    }
}
