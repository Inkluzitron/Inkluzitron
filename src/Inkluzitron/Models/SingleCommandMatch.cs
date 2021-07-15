using Discord.Commands;

namespace Inkluzitron.Models
{
    public class SingleCommandMatch
    {
        public CommandMatch CommandMatch { get; }
        public ParseResult ParseResult { get; }

        public SingleCommandMatch(CommandMatch commandMatch, ParseResult parseResult)
        {
            CommandMatch = commandMatch;
            ParseResult = parseResult;
        }
    }
}
