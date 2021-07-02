using Discord.Commands;
using Inkluzitron.Models.Settings;
using Inkluzitron.Modules.Vote;
using System.Threading.Tasks;

namespace Inkluzitron.Modules
{
    [Name("Hlasování")]
    [Summary("TBD")]
    public class VoteModule : ModuleBase
    {
        private VoteService VoteService { get; }
        private ReactionSettings ReactionSettings { get; }


        public VoteModule(VoteService voteService, VoteDefinitionParser parser, ReactionSettings reactionSettings)
        {
            VoteService = voteService;
            ReactionSettings = reactionSettings;
        }

        [Command("vote")]
        public async Task Vote([Remainder] string _)
            => await VoteService.ProcessVoteCommandAsync(Context.Message);
    }
}
