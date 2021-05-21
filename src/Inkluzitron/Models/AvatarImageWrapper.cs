using System;
using System.Drawing;

namespace Inkluzitron.Models
{
    public class AvatarImageWrapper : IDisposable
    {
        public Image Image { get; }
        public long FileSize { get; }
        public string Extension { get; }

        public AvatarImageWrapper(Image image, long fileSize, string extension)
        {
            Image = image;
            FileSize = fileSize;
            Extension = extension;
        }

        public void Dispose()
        {
            Image.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
