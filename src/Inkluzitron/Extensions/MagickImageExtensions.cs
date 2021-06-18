using ImageMagick;

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
            image.Composite(mask, CompositeOperator.Multiply);
        }

        static public IMagickImage<byte> ToGenericAlphaImage(this IMagickImage<byte> image)
        {
            var pngImage = new MagickImage(MagickColors.Transparent, image.Width, image.Height);
            pngImage.Composite(image, CompositeOperator.Src);
            return pngImage;
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

            using var textArea = new MagickImage($@"pango:<span
                size=""{fontPointSize * 1000}""
                font_family=""{ font.Family }""
                stretch=""{font.Stretch}""
                style=""{(font.Style == FontStyleType.Any ? FontStyleType.Normal : font.Style)}""
                weight=""{font.Weight}""
                foreground=""white""
                >{text}</span>", settings);

            using var colored = new MagickImage(MagickColors.Transparent, textArea.Width, textArea.Height);
            new Drawables().FillColor(foreground).Rectangle(0, 0, colored.Width, colored.Height).Draw(colored);
            colored.Composite(textArea, CompositeOperator.Multiply, Channels.Alpha);

            image.Composite(colored, x, y, CompositeOperator.Over);
        }
    }
}
