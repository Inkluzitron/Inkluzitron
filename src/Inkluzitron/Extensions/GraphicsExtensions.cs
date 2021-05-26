using System;
using System.Drawing;

namespace Inkluzitron.Extensions
{
    static public class GraphicsExtensions
    {
        private const string Ellipsis = "...";

        static public SizeF MeasureAndShrinkText(this Graphics g, ref string text, Font font, float maxWidth, bool appendEllipsis = false)
        {
            if (text is null)
                throw new ArgumentNullException(nameof(text));
            if (font is null)
                throw new ArgumentNullException(nameof(font));

            var textSize = g.MeasureString(text, font);
            var textShortened = false;
            var ellipsisSize = appendEllipsis ? g.MeasureString(Ellipsis, font) : SizeF.Empty;

            while (text.Length > 0 && textSize.Width > maxWidth)
            {
                text = text.Substring(0, text.Length - 1);
                textShortened = true;
                textSize = g.MeasureString(text, font);

                if (appendEllipsis)
                    textSize.Width += ellipsisSize.Width;
            }

            if (appendEllipsis && textShortened)
                text += Ellipsis;

            return textSize;
        }
    }
}
