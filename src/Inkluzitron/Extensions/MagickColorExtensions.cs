using ImageMagick;
using System;

namespace Inkluzitron.Extensions
{
    static public class MagickColorExtensions
    {
        static public double GetPerceivedLuminance(this IMagickColor<byte> color)
        {
            // https://stackoverflow.com/a/596243/3430085
            var r = color.R / 255.0;
            var g = color.G / 255.0;
            var b = color.B / 255.0;

            return Math.Sqrt(0.299 * r * r + 0.587 * g * g + 0.114 * b * b);
        }
    }
}
