using Inkluzitron.Data.Entities;
using Inkluzitron.Enums;
using System;

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

            return ValueNumber.Value switch
            {
                0 => options[0],
                1 or 2 or 3 or 4 => options[1],
                _ => options[2]
            };
        }
    }
}
