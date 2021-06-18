using ImageMagick;

namespace Inkluzitron.Extensions
{
    static public class DrawablesExtensions
    {
        static public IDrawables<byte> Font(this IDrawables<byte> drawable, DrawableFont font)
            => drawable.Font(font.Family, font.Style, font.Weight, font.Stretch);

        static public IDrawables<byte> FontPointSize(
            this IDrawables<byte> drawable, DrawableFontPointSize fontPointSize)
            => drawable.FontPointSize(fontPointSize.PointSize);
    }
}
