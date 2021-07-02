using System;

namespace Inkluzitron.Extensions
{
    static public class DateTimeOffsetExtensions
    {
        static public long ConvertDateTimeOffsetToLong(this DateTimeOffset value)
        {
            if (value < DateTimeOffset.UnixEpoch)
                throw new ArgumentException("The value is too far in the past.", nameof(value));

            return (long)Math.Round((value - DateTimeOffset.UnixEpoch).TotalMilliseconds);
        }
    }
}
