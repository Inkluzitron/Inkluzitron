using Discord;
using Discord.WebSocket;
using Inkluzitron.Contracts;
using Inkluzitron.Data.Entities;
using Inkluzitron.Enums;
using Inkluzitron.Models.Vote;
using System;
using System.Threading.Tasks;

namespace Inkluzitron.Modules.Vote
{
    public class EndOfVotingScheduledTaskHandler : IScheduledTaskHandler
    {
        private VoteService VoteService { get; }
        private DiscordSocketClient Client { get; }

        public EndOfVotingScheduledTaskHandler(VoteService voteService, DiscordSocketClient client)
        {
            VoteService = voteService;
            Client = client;
        }

        public async Task<ScheduledTaskResult> HandleAsync(ScheduledTask scheduledTask)
        {
            if (scheduledTask.Discriminator != EndOfVotingScheduledTask.Identifier)
                return ScheduledTaskResult.NotHandled;

            var data = scheduledTask.ParseData<EndOfVotingScheduledTask>();
            var channel = Client.GetGuild(data.GuildId)?.GetTextChannel(data.ChannelId);
            if (channel is null)
                throw new InvalidOperationException("Unable to locate guild or channel");

            if (await channel.GetMessageAsync(data.MessageId) is not IUserMessage message)
                return ScheduledTaskResult.HandledAndCompleted; // vote doesnt exist anymore

            if (await VoteService.ParseVoteCommand(message) is not VoteDefinition voteDefinition)
                return ScheduledTaskResult.HandledAndCompleted; // it is not a vote anymore

            if (voteDefinition.Deadline is not DateTimeOffset deadline)
                return ScheduledTaskResult.HandledAndCompleted; // the vote no longer has a deadline

            if (!voteDefinition.IsPastDeadline())
            {
                scheduledTask.When = deadline;
                return ScheduledTaskResult.HandledAndPostponed; // the vote should not be ended yet
            }

            var summary = VoteService.ComposeSummary(message, voteDefinition);
            await VoteService.UpdateVoteReplyAsync(message, summary);
            return ScheduledTaskResult.HandledAndCompleted;
        }
    }
}
