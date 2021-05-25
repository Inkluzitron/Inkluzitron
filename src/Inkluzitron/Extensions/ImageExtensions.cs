using System.Collections.Generic;
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

        static public Image ResizeImage(this Image original, Size size) => ResizeImage(original, size.Width, size.Height);

        /// <summary>
        /// Resizes image
        /// </summary>
        /// <remarks>https://stackoverflow.com/a/24199315</remarks>
        static public Image ResizeImage(this Image original, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            using (var graphics = Graphics.FromImage(destImage))
            {
                using var wrapMode = new ImageAttributes();
                wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                graphics.DrawImage(original, destRect, 0, 0, original.Width, original.Height, GraphicsUnit.Pixel, wrapMode);
            }

            return destImage;
        }

        static public Image CropImage(this Image image, Rectangle screen)
        {
            var result = new Bitmap(screen.Width, screen.Height);
            using var graphics = Graphics.FromImage(result);

            graphics.DrawImage(image, 0, 0, screen, GraphicsUnit.Pixel);
            return result;
        }

        static public List<Image> SplitGifIntoFrames(this Image image)
        {
            var frames = new List<Image>();

            for (int i = 0; i < image.GetFrameCount(FrameDimension.Time); i++)
            {
                image.SelectActiveFrame(FrameDimension.Time, i);
                frames.Add(new Bitmap(image));
            }

            return frames;
        }

        static public int CalculateGifDelay(this Image image)
        {
            var item = image.GetPropertyItem(0x5100); // FrameDelay in libgdi+.
            return item.Value[0] + (item.Value[1] * 256);
        }

        static public void RenderRectangle(this Graphics graphics, Rectangle rect, Color color, int radius = 1)
        {
            using var path = new GraphicsPath();

            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            path.AddLine(rect.X + radius, rect.Y, rect.X + rect.Width - radius, rect.Y);
            path.AddArc(rect.X + rect.Width - (2 * radius), rect.Y, 2 * radius, 2 * radius, 270, 90);
            path.AddLine(rect.X + rect.Width, rect.Y + radius, rect.X + rect.Width, rect.Y + rect.Height - radius);
            path.AddArc(rect.X + rect.Width - (2 * radius), rect.Y + rect.Height - (2 * radius), radius + radius, radius + radius, 0, 91);
            path.AddLine(rect.X + radius, rect.Y + rect.Height, rect.X + rect.Width - radius, rect.Y + rect.Height);
            path.AddArc(rect.X, rect.Y + rect.Height - (2 * radius), 2 * radius, 2 * radius, 90, 91);

            using var brush = new SolidBrush(color);
            graphics.FillPath(brush, path);
        }
    }
}
