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
        {
            var settings = new MagickReadSettings()
            {
                BackgroundColor = MagickColors.Transparent,
                Width = maxWidth
            };

            //testTextSettings.SetDefine("pango:wrap", "char");
            if (ellipsize)
                settings.SetDefine("pango:ellipsize", "end");

            using var textArea = new MagickImage($@"pango:<span
                size=""{fontPointSize * 1000}""
                font_family=""{ font.Family }""
                stretch=""{font.Stretch}""
                style=""{(font.Style == FontStyleType.Any ? FontStyleType.Normal : font.Style)}""
                weight=""{font.Weight}""
                foreground=""{foreground.ToHexString()}""
                >{text}</span>", settings);

            image.Composite(textArea, x, y, CompositeOperator.Over);

        }
    }
}
