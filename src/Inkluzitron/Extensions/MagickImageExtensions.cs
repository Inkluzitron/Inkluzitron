using ImageMagick;
using System.Linq;
using System.Security;

namespace Inkluzitron.Extensions
{
    static public class MagickImageExtensions
    {
        static public void RoundImage(this IMagickImage<byte> image)
        {
            using var mask = new MagickImage(MagickColors.Transparent, image.Width, image.Height);
            new Drawables()
                .FillColor(MagickColors.White)
                .Circle(image.Width / 2, image.Height / 2, image.Width / 2, 0)
                .Draw(mask);

            image.Alpha(AlphaOption.On);
            image.Composite(mask, CompositeOperator.Multiply);
        }

        static public IMagickColor<byte> GetDominantColor(this IMagickImage<byte> image)
        {
            using var img = image.Clone();
            img.InterpolativeResize(32, 32, PixelInterpolateMethod.Average);
            img.Quantize(new QuantizeSettings() { Colors = 8 });
            var histogram = img.Histogram();

            var dominantColor = histogram.Aggregate((x, y) => x.Value > y.Value ? x : y).Key;

            return dominantColor;
        }

        static public void DrawEnhancedText(
            this IMagickImage<byte> image, string text, int x, int y, MagickColor foreground,
            DrawableFont font, double fontPointSize, int maxWidth, bool ellipsize = true)
            => DrawEnhancedText(image, text, Gravity.Undefined, x, y, foreground, font, fontPointSize, maxWidth, ellipsize);

        static public void DrawEnhancedText(
            this IMagickImage<byte> image, string text, Gravity gravity, int x, int y, MagickColor foreground,
            DrawableFont font, double fontPointSize, int maxWidth, bool ellipsize = true)
        {
            var settings = new MagickReadSettings()
            {
                BackgroundColor = MagickColors.Transparent,
                Width = maxWidth,
                TextGravity = gravity,
                TextAntiAlias = false
            };

            //settings.SetDefine("pango:wrap", "char");
            if (ellipsize)
                settings.SetDefine("pango:ellipsize", "end");

            // Escape text for use in pango markup language
            // For some reason the text must be excaped twice otherwise it will not work
            text = SecurityElement.Escape(SecurityElement.Escape(text));

            using var textArea = new MagickImage($@"pango:<span
                size=""{fontPointSize * 1000}""
                font_family=""{ font.Family }""
                stretch=""{font.Stretch}""
                style=""{(font.Style == FontStyleType.Any ? FontStyleType.Normal : font.Style)}""
                weight=""{font.Weight}""
                foreground=""white""
                >{text}</span>", settings);

            using var colored = new MagickImage(foreground, textArea.Width, textArea.Height);
            colored.Alpha(AlphaOption.On);
            colored.Composite(textArea, CompositeOperator.Multiply, Channels.Alpha);

            image.Composite(colored, x, y, CompositeOperator.Over);
        }
    }
}
