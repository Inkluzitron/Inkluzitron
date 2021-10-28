using System;

namespace Inkluzitron.Extensions
{
    static public class DoubleExtensions
    {
        static public int ToIntPercentage(this double x)
            => (int)Math.Ceiling(100 * x);
    }
}
