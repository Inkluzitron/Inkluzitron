using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace Inkluzitron.Handlers
{
    /// <summary>
    /// Handler to catch events about commands (CommandExecuted, ...)
    /// </summary>
    public class CommandsHandler : IHandler
    {
        private CommandService CommandService { get; }

        public CommandsHandler(CommandService commandService)
        {
            CommandService = commandService;

            CommandService.CommandExecuted += CommandExecutedAsync;
        }

        private async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            if (!result.IsSuccess && result.Error != null)
            {
                switch (result.Error.Value)
                {
                    case CommandError.UnmetPrecondition:
                    case CommandError.ParseFailed:
                    case CommandError.Unsuccessful:
                    case CommandError.BadArgCount:
                    case CommandError.ObjectNotFound when result is ParseResult parseResult && typeof(IUser).IsAssignableFrom(parseResult.ErrorParameter.Type):
                        await context.Channel.SendMessageAsync(result.ErrorReason);
                        break;
                }
            }
        }
    }
}
