using System;

namespace Inkluzitron.Extensions
{
    static public class DoubleExtensions
    {
        static public int ToIntPercentage(this double x)
            => (int)Math.Round(100 * x);
    }
}
