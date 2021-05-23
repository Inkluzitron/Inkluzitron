using System;
using System.Collections.Generic;
using System.Text;

namespace Inkluzitron.Extensions
{
    static public class StringExtensions
    {
        static public IEnumerable<string> SplitToParts(this StringBuilder builder, int partLength, string separator = "\n")
            => builder.ToString().SplitToParts(partLength, separator);

        static public IEnumerable<string> SplitToParts(this string str, int partLength, string separator = "\n")
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str));

            if (partLength <= 0)
                throw new ArgumentException("Part length has to be positive", nameof(partLength));

            return SplitParts(str, partLength, separator);
        }

        static private IEnumerable<string> SplitParts(string str, int partLength, string separator)
        {
            if (str.Length <= partLength)
            {
                yield return str;
                yield break;
            }

            var currentChunk = new StringBuilder();

            foreach (var item in str.Split(separator, StringSplitOptions.RemoveEmptyEntries))
            {
                if (currentChunk.Length + item.Length + separator.Length > partLength)
                {
                    // Next chunk is over limit, send data and clear buffers.
                    yield return currentChunk.ToString();
                    currentChunk.Clear();
                }

                currentChunk.Append(item).Append(separator);
            }

            yield return currentChunk.ToString();
        }

        static public string Cut(this string str, int maxLength)
        {
            if (str.Length >= maxLength - 3)
                str = str[^(maxLength - 3)] + "...";
            return str;

        }
    }
}
