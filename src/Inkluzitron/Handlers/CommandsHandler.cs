using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Inkluzitron.Handlers
{
    /// <summary>
    /// Handler to catch events about commands (CommandExecuted, ...)
    /// </summary>
    public class CommandsHandler : IHandler
    {
        private CommandService CommandService { get; }

        private IConfiguration Configuration { get; }

        private Random Random { get; }

        public CommandsHandler(Random random, CommandService commandService, IConfiguration configuration)
        {
            CommandService = commandService;

            CommandService.CommandExecuted += CommandExecutedAsync;

            Configuration = configuration;

            Random = random;
        }

        private string GetCommandFormat(CommandInfo command, ParameterInfo highlightArg = null)
        {
            var prefix = Configuration["Prefix"];

            var reply = $"{prefix}{command.Name}";
            foreach (var param in command.Parameters)
            {
                reply += " ";
                if (param == highlightArg) reply += "**__";
                reply += $"{(param.IsOptional ? "[" : "")}{param.Summary ?? param.Name}{(param.IsOptional ? $"]" : "")}";
                if (param == highlightArg) reply += "__**";
            }

            return reply;
        }

        private async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            if (!result.IsSuccess && result.Error != null)
            {
                string reply = "";

                switch (result.Error.Value)
                {
                    case CommandError.UnmetPrecondition:
                    case CommandError.Unsuccessful:
                        reply = result.ErrorReason;
                        break;

                    case CommandError.ObjectNotFound when result is ParseResult parseResult && typeof(IUser).IsAssignableFrom(parseResult.ErrorParameter.Type):
                        ParameterInfo param = ((ParseResult)result).ErrorParameter;
                        var pos = param.Command.Parameters.ToList().IndexOf(param);

                        reply = $"Nemohl jsem najít uživatele zadaného v {(pos + 1)}. argumentu.\n> {GetCommandFormat(command.Value, param)}";
                        break;

                    case CommandError.ParseFailed:
                        param = ((ParseResult)result).ErrorParameter;
                        pos = param.Command.Parameters.ToList().IndexOf(param);

                        var typeName = param.Type.Name;

                        if (typeName == "Int32") typeName = "číslo";
                        if (typeName == "String") typeName = "řetězec";

                        reply = $"V {(pos+1)}. argumentu má být **{typeName}**\n> {GetCommandFormat(command.Value, param)}";
                        break;

                    case CommandError.BadArgCount:
                        var prefix = Configuration["Prefix"];
                        var firstLine = Configuration.GetSection("BadArgumentFirstLine").AsEnumerable().Where(o => o.Value != null).ToArray();
                        
                        reply = $"{firstLine[Random.Next(firstLine.Length)].Value}\n> {GetCommandFormat(command.Value)}";
                        break;

                    default:
                        return;
                }

                // Reply to command message and mention only replied user 
                await context.Message.ReplyAsync(reply, allowedMentions: new AllowedMentions {MentionRepliedUser = true });
            }
        }
    }
}
