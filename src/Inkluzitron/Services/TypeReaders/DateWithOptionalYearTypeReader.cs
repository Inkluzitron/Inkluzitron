using Discord.Commands;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Inkluzitron.Services.TypeReaders
{
    public class DateWithOptionalYearTypeReader : TypeReader
    {
        static public readonly int FallbackYear = DateTime.MinValue.Year;

        public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
        {
            const DateTimeStyles styles = DateTimeStyles.AssumeLocal;
            var culture = CultureInfo.InvariantCulture;

            input = Regex.Replace(input, "\\s+", string.Empty);
            DateTime? result = null;

            if (DateTime.TryParseExact(input, "d/M", culture, styles, out var dateWithoutYear))
                result = new DateTime(FallbackYear, dateWithoutYear.Month, dateWithoutYear.Day, 0, 0, 0, DateTimeKind.Local);
            else if (DateTime.TryParseExact(input, "d'/'M'/'yyyy", culture, styles, out var ordinarilySeparatedDate))
                result = ordinarilySeparatedDate;
            else if (DateTime.TryParseExact(input, "d'.'M'.'", culture, styles, out var dotSeparatedDateWithoutYear))
                result = new DateTime(FallbackYear, dotSeparatedDateWithoutYear.Month, dotSeparatedDateWithoutYear.Day, 0, 0, 0, DateTimeKind.Local);
            else if (DateTime.TryParseExact(input, "d'.'M'.'yyyy", culture, styles, out var dotSeparatedDate))
                result = dotSeparatedDate;

            return Task.FromResult(
                result is DateTime successfullyParsedDate
                ? TypeReaderResult.FromSuccess(successfullyParsedDate)
                : TypeReaderResult.FromError(CommandError.ParseFailed, $"Nelze přečíst datum '{input}'.")
            );
        }
    }
}
