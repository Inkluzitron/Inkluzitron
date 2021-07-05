using Discord;
using Discord.WebSocket;
using Inkluzitron.Contracts;
using Inkluzitron.Data.Entities;
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

        public async Task<bool> HandleAsync(ScheduledTask scheduledTask)
        {
            if (scheduledTask.Discriminator != "EndOfVoting")
                return false;

            var data = scheduledTask.ParseData<EndOfVotingScheduledTaskData>();
            var channel = Client.GetGuild(data.GuildId)?.GetTextChannel(data.ChannelId);
            if (channel is null)
                throw new InvalidOperationException("Unable to locate guild or channel");

            var message = await channel.GetMessageAsync(data.MessageId) as IUserMessage;
            if (message is null)
                return true; // vote doesnt exist anymore

            if (await VoteService.ParseVoteCommand(message) is not VoteDefinition voteDefinition)
                return true; // it is not a vote anymore

            if (!voteDefinition.IsPastDeadline())
                return true; // vote has an updated deadline, should not be finished yet

            var summary = VoteService.ComposeSummary(message, voteDefinition);
            await VoteService.UpdateVoteReplyAsync(message, summary);
            return true;
        }
    }
}
