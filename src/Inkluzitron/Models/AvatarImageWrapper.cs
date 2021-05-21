using System;
using System.Drawing;

namespace Inkluzitron.Models
{
    public class AvatarImageWrapper : IDisposable
    {
        private bool ImageShouldBeDisposed { get; }
        public Image Image { get; }
        public long FileSize { get; }
        public string Extension { get; }

        public AvatarImageWrapper(Image image, long fileSize, string extension, bool imageShouldBeDisposed = true)
        {
            Image = image;
            FileSize = fileSize;
            Extension = extension;
            ImageShouldBeDisposed = imageShouldBeDisposed;
        }

        public void Dispose()
        {
            if (ImageShouldBeDisposed)
                Image.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
