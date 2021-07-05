using Inkluzitron.Data.Entities;
using Inkluzitron.Enums;
using System;
using System.Text.RegularExpressions;

namespace Inkluzitron.Utilities
{
    public class FormatByValue : IFormattable
    {
        private ulong? ValueNumber { get; }
        private Gender? ValueGender { get; }

        public FormatByValue(int value) => ValueNumber = (ulong)Math.Abs(value);
        public FormatByValue(long value) => ValueNumber = (ulong)Math.Abs(value);
        public FormatByValue(uint value) => ValueNumber = value;
        public FormatByValue(ulong value) => ValueNumber = value;

        public FormatByValue(Gender value) => ValueGender = value;
        public FormatByValue(User value) => ValueGender = value.Gender;

        public string ToString(string format, IFormatProvider formatProvider = null)
        {
            var options = format.Split(':');
            if (options.Length < 3)
                throw new ArgumentException("Format string must be provided as a:b:c", nameof(format));

            if (ValueGender.HasValue)
            {
                return ValueGender.Value switch
                {
                    Gender.Male => options[0],
                    Gender.Female => options[1],
                    _ => options[2]
                };
            }

            if (options.Length == 3 && options[0] != "ext")
            {
                return ValueNumber.Value switch
                {
                    0 => options[0],
                    1 or 2 or 3 or 4 => options[1],
                    _ => options[2]
                };
            }
            else
            {
                options = options[1..];

                var lookupTable = options[0].Split(",");
                if (lookupTable.Length != options.Length - 1)
                    throw new ArgumentException("Format string '" + format + "' has an unexpected number of items in the lookup table");

                int? destinationIndex = default;
                for (var i = 0; i < lookupTable.Length; i++)
                {
                    if (ValueNumber.Value.ToString() == lookupTable[i])
                    {
                        destinationIndex = i + 1;
                        break;
                    }
                }

                if (destinationIndex is not int chosenIndex || chosenIndex >= options.Length)
                    destinationIndex = null;

                return options[destinationIndex ?? options.Length - 1];
            }
        }
    }
}
