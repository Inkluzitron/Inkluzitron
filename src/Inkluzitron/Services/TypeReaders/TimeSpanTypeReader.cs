using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Inkluzitron.Services.TypeReaders
{
    public class TimeSpanTypeReader : TypeReader
    {
        private const RegexOptions regexOptions = RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace;

        private Dictionary<Regex, Func<Match, TimeSpan>> MatchingFunctions { get; } = new()
        {
            { new Regex(@"(\d+)\s*(dn[ůíy]?|d)", regexOptions), m => TimeSpan.FromDays(int.Parse(m.Groups[1].Value)) },
            { new Regex(@"(\d+)\s*(hodin[auy]?|hod|h)", regexOptions), m => TimeSpan.FromHours(int.Parse(m.Groups[1].Value)) },
            { new Regex(@"(\d+)\s*(minut[auy]?|min|m)", regexOptions), m => TimeSpan.FromMinutes(int.Parse(m.Groups[1].Value)) },
            { new Regex(@"(\d+)\s*(sekund[auy]?|sec|s)", regexOptions), m => TimeSpan.FromSeconds(int.Parse(m.Groups[1].Value)) },

            { new Regex(@"(^|\s+)(1\s+den|den|dny)($|\s+)", regexOptions), _ => TimeSpan.FromDays(1) },
            { new Regex(@"(^|\s+)hodin[auy]($|\s+)", regexOptions), _ => TimeSpan.FromHours(1) },
            { new Regex(@"(^|\s+)minut[auy]($|\s+)", regexOptions), _ => TimeSpan.FromMinutes(1) },
            { new Regex(@"(^|\s+)sekund[auy]($|\s+)", regexOptions), _ => TimeSpan.FromSeconds(1) }
        };

        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            if (TimeSpan.TryParse(input, CultureInfo.GetCultureInfo("cs-CZ"), out var timeSpan))
                return Task.FromResult(TypeReaderResult.FromSuccess(timeSpan));

            TimeSpan? result = default;

            while (input.Length > 0)
            {
                var somethingFound = false;

                foreach (var func in MatchingFunctions)
                {
                    var match = func.Key.Match(input);

                    if (match.Success)
                    {
                        somethingFound = true;
                        result = (result ?? TimeSpan.Zero) + func.Value(match);

                        var newInput = input.Substring(0, match.Index);
                        var after = match.Index + match.Value.Length;

                        if (after < input.Length)
                            newInput += input.Substring(after);

                        input = newInput.Trim();
                        break;
                    }
                }

                if (!somethingFound)
                {
                    result = default;
                    break;
                }
            }

            if (result.HasValue)
                return Task.FromResult(TypeReaderResult.FromSuccess(result.Value));
            else
                return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Čas není ve správném formátu."));
        }
    }
}
