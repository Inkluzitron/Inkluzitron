using Discord.Commands;
using Inkluzitron.Data;
using Inkluzitron.Models.Settings;
using Inkluzitron.Modules.Vote;
using Inkluzitron.Services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inkluzitron.Modules
{
    [Name("Hlasování")]
    [Summary("TBD")]
    public class VoteModule : ModuleBase
    {
        private DatabaseFactory DatabaseFactory { get; }
        private ScheduledTasksService ScheduledTasks { get; }


        public VoteModule(DatabaseFactory databaseFactory, ScheduledTasksService scheduledTasks)
        {
            DatabaseFactory = databaseFactory;
            ScheduledTasks = scheduledTasks;
        }

        [Command("ohýnek")]
        public async Task Ohýnek()
        {
            await ScheduledTasks.EnqueueAsync(new Data.Entities.ScheduledTask
            {
                Discriminator = "EndOfVoting",
                When = DateTimeOffset.UtcNow.AddSeconds(10),
                Data = JsonConvert.SerializeObject(new EndOfVotingScheduledTaskData
                {
                    GuildId = Context.Guild.Id,
                    ChannelId = Context.Channel.Id,
                    MessageId = Context.Message.Id
                })
            });

            await ReplyAsync("Tak si počkej.");
        }
    }
}
