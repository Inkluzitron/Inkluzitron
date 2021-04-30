using Discord.Commands;

namespace Inkluzitron.Models
{
    public class CommandRedirectResult : RuntimeResult
    {
        public string NewCommand { get; set; }

        public CommandRedirectResult(string newCommand) : base(CommandError.Unsuccessful, null)
        {
            NewCommand = newCommand;
        }

        public CommandRedirectResult(CommandError? error, string reason) : base(error, reason)
        {
        }
    }
}
