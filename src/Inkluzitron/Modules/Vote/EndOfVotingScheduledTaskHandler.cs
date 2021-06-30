using Discord.WebSocket;
using Inkluzitron.Contracts;
using Inkluzitron.Data.Entities;
using Inkluzitron.Extensions;
using System.Threading.Tasks;

namespace Inkluzitron.Modules.Vote
{
    public class EndOfVotingScheduledTaskHandler : IScheduledTaskHandler
    {
        private DiscordSocketClient Client { get; }

        public EndOfVotingScheduledTaskHandler(DiscordSocketClient client)
        {
            Client = client;
        }

        public async Task<bool> TryHandleAsync(ScheduledTask scheduledTask)
        {
            if (scheduledTask.Discriminator != "EndOfVoting")
                return false;

            var data = scheduledTask.ParseData<EndOfVotingScheduledTaskData>();
            var message = await Client.GetGuild(data.GuildId)
                .GetTextChannel(data.ChannelId)
                .GetMessageAsync(data.MessageId);

            /*if (message is null)
                return true;*/

            await message.AddReactionAsync("🔥".ToDiscordEmote());
            return true;
        }
    }
}
