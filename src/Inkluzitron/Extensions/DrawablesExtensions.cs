using ImageMagick;
using System.Collections.Generic;

namespace Inkluzitron.Extensions
{
    static public class DrawablesExtensions
    {
        static public IDrawables<byte> Font(this IDrawables<byte> drawable, DrawableFont font)
            => drawable.Font(font.Family, font.Style, font.Weight, font.Stretch);

        static public IDrawables<byte> FontPointSize(
            this IDrawables<byte> drawable, DrawableFontPointSize fontPointSize)
            => drawable.FontPointSize(fontPointSize.PointSize);

        static public IDrawables<byte> Lines(this IDrawables<byte> drawable, PointD[] points)
        {
            for (int i = 1; i < points.Length; i++)
            {
                drawable = drawable.Line(
                    points[i - 1].X, points[i - 1].Y,
                    points[i].X, points[i].Y);
            }

            return drawable;
        }
    }
}
