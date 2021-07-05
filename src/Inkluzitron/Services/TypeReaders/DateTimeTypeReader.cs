﻿using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Inkluzitron.Services.TypeReaders
{
    public class DateTimeTypeReader : TypeReader
    {
        private const RegexOptions regexOptions = RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.IgnorePatternWhitespace;

        private Dictionary<Regex, Func<DateTime>> MatchingFunctions { get; } = new()
        {
            { new Regex("^(today|dnes(ka)?)$", regexOptions), () => DateTime.Today }, // today, dnes, dneska
            { new Regex("^(tommorow|z[ií]tra|zajtra)$", regexOptions), () => DateTime.Now.AddDays(1) }, // tommorow, zítra, zitra, zajtra
            { new Regex("^(v[cč]era|yesterday)$", regexOptions), () => DateTime.Now.AddDays(-1) }, // vcera, včera, yesterday
            { new Regex("^(poz[ií]t[rř][ií]|pozajtra)$", regexOptions), () => DateTime.Now.AddDays(2) }, // pozítří, pozitri, pozajtra
            { new Regex("^(te[dď]|now|teraz)$", regexOptions), () => DateTime.Now } // teď, ted, now, teraz
        };

        private Regex TimeShiftRegex { get; } = new(@"(\d+)(m|h|d|M|y|r)", regexOptions);

        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            // US dates use '/' as delimeter. We use this fact to detect american dates and parse them correctly (MM/DD instead of DD.MM)
            if (input.Contains('/') && DateTime.TryParse(input, new CultureInfo("en-US"), DateTimeStyles.None, out DateTime dateTime))
                return Task.FromResult(TypeReaderResult.FromSuccess(dateTime));

            if (DateTime.TryParse(input, CultureInfo.CurrentCulture, DateTimeStyles.None, out dateTime))
                return Task.FromResult(TypeReaderResult.FromSuccess(dateTime));

            foreach (var func in MatchingFunctions)
            {
                if (func.Key.IsMatch(input))
                    return Task.FromResult(TypeReaderResult.FromSuccess(func.Value()));
            }

            var timeShift = TimeShiftRegex.Match(input);
            var timeShiftMatched = timeShift.Success;
            var result = DateTime.Now;
            while (timeShift.Success)
            {
                var timeValue = Convert.ToInt32(timeShift.Groups[1].Value);

                switch (timeShift.Groups[2].Value)
                {
                    case "m": // minutes
                        result = result.AddMinutes(timeValue);
                        break;
                    case "h": // hours
                        result = result.AddHours(timeValue);
                        break;
                    case "d": // days
                        result = result.AddDays(timeValue);
                        break;
                    case "M": // months
                        result = result.AddMonths(timeValue);
                        break;
                    case "r":
                    case "y": // years
                        result = result.AddYears(timeValue);
                        break;
                }

                timeShift = timeShift.NextMatch();
            }

            return timeShiftMatched
                ? Task.FromResult(TypeReaderResult.FromSuccess(result))
                : Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Datum a čas není ve správném formátu."));
        }
    }
}
