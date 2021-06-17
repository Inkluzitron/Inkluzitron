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

        static public IMagickImage<byte> ToPng(this IMagickImage<byte> image)
        {
            var pngImage = new MagickImage(MagickColors.Transparent, image.Width, image.Height);
            pngImage.Composite(image, CompositeOperator.Src);
            return pngImage;
        }
    }
}
