using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inkluzitron.Extensions
{
    static public class TimeSpanExtensions
    {
        static public string FullTextFormat(this TimeSpan timeSpan)
        {
            var parts = new List<string>();

            if (timeSpan.Days > 0)
            {
                if (timeSpan.Days > 4) parts.Add($"{timeSpan.Days} dní");
                else if (timeSpan.Days == 1) parts.Add("1 den");
                else parts.Add($"{timeSpan.Days} dny");
            }

            if (timeSpan.Days > 0 || timeSpan.Hours > 0)
            {
                if (timeSpan.Hours == 0 || timeSpan.Hours > 4) parts.Add($"{timeSpan.Hours} hodin");
                else if (timeSpan.Hours == 1) parts.Add("1 hodinu");
                else parts.Add($"{timeSpan.Hours} hodiny");
            }

            if (timeSpan.Hours > 0 || timeSpan.Minutes > 0)
            {
                if (timeSpan.Minutes == 0 || timeSpan.Minutes > 4) parts.Add($"{timeSpan.Minutes} minut");
                else if (timeSpan.Minutes == 1) parts.Add("1 minutu");
                else parts.Add($"{timeSpan.Minutes} minuty");
            }

            if (timeSpan.Minutes > 0 || timeSpan.Seconds > 0)
            {
                if (timeSpan.Seconds == 0 || timeSpan.Seconds > 4) parts.Add($"{timeSpan.Seconds} vteřin");
                else if (timeSpan.Seconds == 1) parts.Add("1 vteřinu");
                else parts.Add($"{timeSpan.Seconds} vteřiny");
            }

            if (parts.Count == 0) return "0 vteřin";
            else if (parts.Count == 1) return parts[0];
            else return $"{string.Join(", ", parts.Take(parts.Count - 1))} a {parts[^1]}";
        }
    }
}
