using Discord;
using Discord.Commands;
using Inkluzitron.Extensions;
using Inkluzitron.Models;
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
        private IServiceProvider Provider { get; }
        private IConfiguration Configuration { get; }

        public CommandsHandler(CommandService commandService, IConfiguration configuration, IServiceProvider provider)
        {
            CommandService = commandService;
            Configuration = configuration;
            Provider = provider;

            CommandService.CommandExecuted += CommandExecutedAsync;
        }

        private async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            // Null is success, because some modules returns null after success and library always returns ExecuteResult.
            if (result == null) result = ExecuteResult.FromSuccess();

            if (!result.IsSuccess && result.Error != null)
            {
                string reply = "";

                string moreInfo = "";
                if (command.IsSpecified)
                    moreInfo = $"\n*Pro vice informací zadej `{Configuration["Prefix"]}help {command.Value.Aliases.First().Split(' ', 2)[0]}`*";

                switch (result.Error.Value)
                {
                    case CommandError.Unsuccessful when result is CommandRedirectResult crr && !string.IsNullOrEmpty(crr.NewCommand):
                        await CommandService.ExecuteAsync(context, crr.NewCommand, Provider);
                        break;

                    case CommandError.UnmetPrecondition:
                    case CommandError.Unsuccessful:
                        reply = result.ErrorReason;
                        break;

                    case CommandError.ObjectNotFound when result is ParseResult parseResult && typeof(IUser).IsAssignableFrom(parseResult.ErrorParameter.Type):
                        ParameterInfo param = ((ParseResult)result).ErrorParameter;
                        var pos = param.Command.Parameters.ToList().IndexOf(param);

                        reply = $"Nemohl jsem najít uživatele zadaného v {pos + 1}. argumentu.\n> {command.Value.GetCommandFormat(Configuration["Prefix"], param)}{moreInfo}";
                        break;

                    case CommandError.ParseFailed:
                        param = ((ParseResult)result).ErrorParameter;
                        pos = param.Command.Parameters.ToList().IndexOf(param);

                        var typeName = param.Type.Name;

                        if (typeName == "Int32") typeName = "číslo";
                        if (typeName == "String") typeName = "řetězec";

                        reply = $"V {pos + 1}. argumentu má být **{typeName}**\n> {command.Value.GetCommandFormat(Configuration["Prefix"], param)}{moreInfo}";
                        break;

                    case CommandError.BadArgCount:
                        var firstLine = Configuration.GetSection("BadArgumentFirstLine").AsEnumerable().Where(o => o.Value != null).ToArray();

                        reply = $"{firstLine[ThreadSafeRandom.Next(firstLine.Length)].Value}\n> {command.Value.GetCommandFormat(Configuration["Prefix"])}{moreInfo}";
                        break;

                    case CommandError.Exception:
                        await context.Message.AddReactionAsync(new Emoji("❌"));
                        break;
                }

                // Reply to command message without mentioning any user
                if (!string.IsNullOrEmpty(reply))
                    await context.Message.ReplyAsync(reply, allowedMentions: new AllowedMentions { MentionRepliedUser = false });
            }
        }
    }
}
