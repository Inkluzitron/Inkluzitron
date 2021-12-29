using Discord.Commands;
using Inkluzitron.Data.Entities;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Inkluzitron.Services.TypeReaders
{
    public class DateWithOptionalYearTypeReader : TypeReader
    {
        static public readonly int FallbackYear = User.UnsetBirthdayYear;
        static private readonly Regex DateMonthPattern = new(@"^([1-9]|(?:1[0-9])|(?:2[0-9])|(?:3[01]))(?:\.|/)([1-9]|(?:1[0-2]))(?:\.|/)$");
        static private readonly Regex DateMonthYearPattern = new(@"^([1-9]|(?:1[0-9])|(?:2[0-9])|(?:3[01]))(?:\.|/)([1-9]|(?:1[0-2]))(?:\.|/)(\d{4})$");

        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            const DateTimeStyles styles = DateTimeStyles.AssumeLocal;
            var culture = CultureInfo.InvariantCulture;

            input = Regex.Replace(input, "\\s+", string.Empty);
            var reassembledDate = string.Empty;

            if (DateMonthYearPattern.Match(input) is Match m1 && m1.Success)
            {
                var day = int.Parse(m1.Groups[1].Value);
                var month = int.Parse(m1.Groups[2].Value);
                var year = int.Parse(m1.Groups[3].Value);
                reassembledDate = $"{year:D4}-{month:D2}-{day:D2}T00:00:00";
            }
            else if (DateMonthPattern.Match(input) is Match m2 && m2.Success)
            {
                var day = int.Parse(m2.Groups[1].Value);
                var month = int.Parse(m2.Groups[2].Value);
                reassembledDate = $"{FallbackYear:D4}-{month:D2}-{day:D2}T00:00:00";
            }

            TypeReaderResult result;
            if (DateTime.TryParseExact(reassembledDate, "s", culture, styles, out var reconstructedDate))
            {
                result = TypeReaderResult.FromSuccess(new DateTime(
                    reconstructedDate.Year, reconstructedDate.Month, reconstructedDate.Day,
                    0, 0, 0,
                    DateTimeKind.Local
                ));
            }
            else
            {
                result = TypeReaderResult.FromError(CommandError.ParseFailed, $"Nelze přečíst datum '{input}'.");
            }

            return Task.FromResult(result);
        }
    }
}
