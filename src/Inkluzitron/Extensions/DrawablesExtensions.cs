using ImageMagick;
using System;

namespace Inkluzitron.Extensions
{
    static public class DrawablesExtensions
    {
        private const string Ellipsis = "...";

        static public IDrawables<byte> Font(this IDrawables<byte> drawable, DrawableFont font)
            => drawable.Font(font.Family, font.Style, font.Weight, font.Stretch);

        static public IDrawables<byte> FontPointSize(
            this IDrawables<byte> drawable, DrawableFontPointSize fontPointSize)
            => drawable.FontPointSize(fontPointSize.PointSize);

        static public ITypeMetric FontTypeMetricsAndShrink(
            this IDrawables<byte> drawable, ref string text, double maxWidth, bool appendEllipsis = false)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));

            var textMetrics = drawable.FontTypeMetrics(text);
            var textShortened = false;

            while (text.Length > 0 && textMetrics.TextWidth > maxWidth)
            {
                text = text.Substring(0, text.Length - 1);
                textShortened = true;
                if (appendEllipsis)
                    textMetrics = drawable.FontTypeMetrics($"{text}{Ellipsis}");
                else
                    textMetrics = drawable.FontTypeMetrics(text);
            }

            if (appendEllipsis && textShortened)
                text += Ellipsis;

            return textMetrics;
        }
    }
}
