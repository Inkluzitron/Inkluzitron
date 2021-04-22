using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Inkluzitron.Extensions
{
    static public class ImageExtensions
    {
        static public Image RoundImage(this Image original)
        {
            using var brush = new TextureBrush(original);

            var rounded = new Bitmap(original.Width, original.Height, original.PixelFormat);
            rounded.MakeTransparent();

            using var g = Graphics.FromImage(rounded);
            using var gp = new GraphicsPath();

            g.Clear(Color.Transparent);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            gp.AddEllipse(0, 0, original.Width, original.Height);
            g.FillPath(brush, gp);

            return rounded;
        }

        /// <summary>
        /// Resizes image
        /// </summary>
        /// <remarks>https://stackoverflow.com/a/24199315</remarks>
        static public Image ResizeImage(this Image original, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(original.HorizontalResolution, original.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using var wrapMode = new ImageAttributes();
                wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                graphics.DrawImage(original, destRect, 0, 0, original.Width, original.Height, GraphicsUnit.Pixel, wrapMode);
            }

            return destImage;
        }
    }
}
