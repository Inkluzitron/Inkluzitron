using Inkluzitron.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Inkluzitron.Models
{
    public class AvatarImageWrapper : IDisposable
    {
        public List<Image> Frames { get; init; }
        public long FileSize { get; init; }
        public string Extension { get; init; }
        public bool IsAnimated { get; init; }

        // Gif specific
        public int? GifDelay { get; init; }

        static public AvatarImageWrapper FromAnimatedImage(Image image, long size, string extension)
        {
            return new AvatarImageWrapper()
            {
                Frames = image.SplitGifIntoFrames(),
                Extension = extension,
                FileSize = size,
                IsAnimated = true,
                GifDelay = image.CalculateGifDelay()
            };
        }

        static public AvatarImageWrapper FromImage(Image image, long size, string extension)
        {
            return new AvatarImageWrapper()
            {
                Extension = extension,
                IsAnimated = false,
                FileSize = size,
                Frames = new() { image }
            };
        }

        public void Dispose()
        {
            Frames.ForEach(o => o.Dispose());
            GC.SuppressFinalize(this);
        }
    }
}
