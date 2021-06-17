using ImageMagick;
using System;

namespace Inkluzitron.Models
{
    public class AvatarImageWrapper : IDisposable
    {
        public MagickImageCollection Frames { get; init; }
        public long FileSize { get; init; }
        public string Extension { get; init; }
        public bool IsAnimated { get; init; }

        static public AvatarImageWrapper FromAnimatedImage(MagickImageCollection image, long size, string extension)
        {
            return new AvatarImageWrapper()
            {
                Frames = image,
                Extension = extension,
                FileSize = size,
                IsAnimated = true
            };
        }

        static public AvatarImageWrapper FromImage(MagickImageCollection image, long size, string extension)
        {
            return new AvatarImageWrapper()
            {
                Extension = extension,
                IsAnimated = false,
                FileSize = size,
                Frames = image
            };
        }

        public void Dispose()
        {
            Frames.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
