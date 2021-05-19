using Discord;
using GrapeCity.Documents.Imaging;
using Inkluzitron.Extensions;
using Inkluzitron.Resources.Bonk;
using Inkluzitron.Resources.Pat;
using Inkluzitron.Resources.Peepoangry;
using Inkluzitron.Resources.Peepolove;
using Inkluzitron.Resources.Spank;
using Inkluzitron.Resources.Whip;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SysDrawImage = System.Drawing.Image;
using SysImgFormat = System.Drawing.Imaging.ImageFormat;

namespace Inkluzitron.Services
{
    public class ImagesService
    {
        private ProfilePictureService ProfilePictureService { get; }

        public ImagesService(ProfilePictureService profilePictureService)
        {
            ProfilePictureService = profilePictureService;
            Directory.CreateDirectory("ImageCache");
        }

        static public string CreateCachePath(string filename)
            => Path.Combine("ImageCache", filename);

        static private List<Bitmap> GetBitmapsFromResources<TResources>()
        {
            return typeof(TResources).GetProperties()
                .Where(o => o.PropertyType == typeof(Bitmap))
                .Select(o => o.GetValue(null, null) as Bitmap)
                .Where(o => o != null)
                .ToList();
        }

        // Taken from https://github.com/sinus-x/rubbergoddess
        public async Task<string> WhipAsync(IUser target, IUser caller)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (caller == null)
                throw new ArgumentNullException(nameof(caller));

            var self = target.Equals(caller);
            var gifName = CreateCachePath($"Whip{(self ? "_self" : "")}_{target.Id}_{target.AvatarId ?? target.Discriminator}.gif");

            if (!File.Exists(gifName))
            {
                var profilePicture = await ProfilePictureService.GetProfilePictureAsync(target);
                using var gifWriter = new GcGifWriter(gifName);
                using var gcBitmap = new GcBitmap();

                if (self) profilePicture.RotateFlip(RotateFlipType.RotateNoneFlipX);

                var frames = GetBitmapsFromResources<WhipResources>();
                for (int i = 0; i < frames.Count; i++)
                {
                    var frame = frames[i];
                    var whipFrame = RenderWhipFrame(profilePicture, frame, i);

                    if (self) whipFrame.RotateFlip(RotateFlipType.RotateNoneFlipX);

                    using var ms = new MemoryStream();
                    whipFrame.Save(ms, SysImgFormat.Png);

                    gcBitmap.Load(ms.ToArray());
                    gifWriter.AppendFrame(gcBitmap, disposalMethod: GifDisposalMethod.RestoreToBackgroundColor);
                }
            }

            return gifName;
        }

        static private Bitmap RenderWhipFrame(SysDrawImage profilePicture, Bitmap frame, int index)
        {
            var deformation = new[] { 0, 0, 0, 0, 0, 0, 0, 0, 2, 3, 5, 9, 6, 4, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var translation = new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 2, 3, 3, 3, 2, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            var bitmap = new Bitmap(250, 145);
            bitmap.MakeTransparent();

            using var frameAvatar = profilePicture.ResizeImage(100 - deformation[index], 100);

            using var g = Graphics.FromImage(bitmap);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            g.DrawImage(frameAvatar, 135 + deformation[index] + translation[index], 30);
            g.DrawImage(frame, 0, -10);

            return bitmap;
        }

        // Taken from https://github.com/sinus-x/rubbergoddess
        public async Task<string> BonkAsync(IUser target, IUser caller)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (caller == null)
                throw new ArgumentNullException(nameof(caller));

            var self = target.Equals(caller);
            var gifName = CreateCachePath($"bonk{(self ? "_self" : "")}_{target.Id}_{target.AvatarId ?? target.Discriminator}.gif");

            if (!File.Exists(gifName))
            {
                var profilePicture = await ProfilePictureService.GetProfilePictureAsync(target);
                using var gifWriter = new GcGifWriter(gifName);
                using var gcBitmap = new GcBitmap();

                if (self) profilePicture.RotateFlip(RotateFlipType.RotateNoneFlipX);

                var frames = GetBitmapsFromResources<BonkResources>();
                for (int i = 0; i < frames.Count; i++)
                {
                    var frame = frames[i];
                    using var bonkFrame = RenderBonkFrame(profilePicture, frame, i);

                    if (self) bonkFrame.RotateFlip(RotateFlipType.RotateNoneFlipX);

                    using var ms = new MemoryStream();
                    bonkFrame.Save(ms, SysImgFormat.Png);

                    gcBitmap.Load(ms.ToArray());
                    gifWriter.AppendFrame(gcBitmap, disposalMethod: GifDisposalMethod.RestoreToBackgroundColor, delayTime: 3);
                }
            }

            return gifName;
        }

        static private Bitmap RenderBonkFrame(SysDrawImage profilePicture, Bitmap frame, int index)
        {
            var bitmap = new Bitmap(250, 170);
            bitmap.MakeTransparent();

            var deformation = new[] { 0, 0, 0, 0, 5, 20, 15, 5 };
            using var frameAvatar = profilePicture.ResizeImage(110, 100 - deformation[index]);

            using var g = Graphics.FromImage(bitmap);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            g.DrawImage(frameAvatar, 100, 60 + deformation[index]);
            g.DrawImage(frame, 0, 0);

            return bitmap;
        }

        // Taken from https://github.com/Misha12/GrillBot
        public async Task<string> PeepoAngryAsync(IUser target, int fileUploadLimit)
        {
            var imageName = CreateCachePath($"Peepoangry_{target.Id}_{target.AvatarId ?? target.Discriminator}.{(target.AvatarId.StartsWith("a_") ? "gif" : "png")}");

            if (!File.Exists(imageName))
            {
                var profilePictureData = await target.DownloadProfilePictureAsync(size: 64);
                using var memStream = new MemoryStream(profilePictureData);
                using var rawProfilePicture = SysDrawImage.FromStream(memStream);

                // Large animated profile pictures have problem with gif upload.
                if (Path.GetExtension(imageName) == ".gif" && profilePictureData.Length >= 2 * (fileUploadLimit / 3))
                    imageName = Path.ChangeExtension(imageName, ".png");

                if (Path.GetExtension(imageName) == ".gif")
                {
                    var frames = rawProfilePicture.SplitGifIntoFrames();

                    try
                    {
                        using var gifWriter = new GcGifWriter(imageName);
                        using var gcBitmap = new GcBitmap();

                        foreach (var userFrame in frames)
                        {
                            using var roundedUserFrame = userFrame.RoundImage();
                            using var frame = RenderPeepoangryFrame(roundedUserFrame);

                            using var ms = new MemoryStream();
                            frame.Save(ms, SysImgFormat.Png);

                            gcBitmap.Load(ms.ToArray());
                            gifWriter.AppendFrame(gcBitmap, disposalMethod: GifDisposalMethod.RestoreToBackgroundColor, delayTime: rawProfilePicture.CalculateGifDelay());
                        }
                    }
                    finally
                    {
                        frames.ForEach(o => o.Dispose());
                        frames.Clear();
                    }
                }
                else if (Path.GetExtension(imageName) == ".png")
                {
                    using var roundedProfileImage = rawProfilePicture.RoundImage();
                    var profilePicture = roundedProfileImage.ResizeImage(64, 64);

                    using var frame = RenderPeepoangryFrame(profilePicture);
                    frame.Save(imageName, SysImgFormat.Png);
                }
            }

            return imageName;
        }

        static private SysDrawImage RenderPeepoangryFrame(SysDrawImage profilePicture)
        {
            var body = new Bitmap(250, 105);
            using var graphics = Graphics.FromImage(body);

            graphics.DrawImage(profilePicture, new Rectangle(new Point(20, 10), new Size(64, 64)));
            graphics.DrawImage(PeepoangryResources.peepoangry, new Point(115, -5));

            return body;
        }

        // Taken from https://github.com/Toaster192/rubbergod
        public async Task<string> PatAsync(IUser target, IUser caller)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (caller == null)
                throw new ArgumentNullException(nameof(caller));

            var self = target.Equals(caller);
            var gifName = CreateCachePath($"Pat{(self ? "_self" : "")}_{target.Id}_{target.AvatarId ?? target.Discriminator}.gif");

            if (!File.Exists(gifName))
            {
                var profilePicture = await ProfilePictureService.GetProfilePictureAsync(target);
                using var gifWriter = new GcGifWriter(gifName);
                using var gcBitmap = new GcBitmap();

                if (self) profilePicture.RotateFlip(RotateFlipType.RotateNoneFlipX);

                var frames = GetBitmapsFromResources<PatResources>();
                for (int i = 0; i < frames.Count; i++)
                {
                    var frame = frames[i];
                    using var bonkFrame = RenderPatFrame(profilePicture, frame, i);

                    if (self) bonkFrame.RotateFlip(RotateFlipType.RotateNoneFlipX);

                    using var ms = new MemoryStream();
                    bonkFrame.Save(ms, SysImgFormat.Png);

                    gcBitmap.Load(ms.ToArray());
                    gifWriter.AppendFrame(gcBitmap, disposalMethod: GifDisposalMethod.RestoreToBackgroundColor, delayTime: 5);
                }
            }

            return gifName;
        }

        static private Bitmap RenderPatFrame(SysDrawImage profilePicture, Bitmap frame, int index)
        {
            var deformation = new[]
            {
                new Point(-1, 4),
                new Point(-2, 3),
                new Point(1, 1),
                new Point(2, 1),
                new Point(1, -4)
            };

            var bitmap = new Bitmap(130, 150);
            bitmap.MakeTransparent();

            using var frameAvatar = profilePicture.ResizeImage(100 - deformation[index].X, 100 - deformation[index].Y);

            using var g = Graphics.FromImage(bitmap);
            g.DrawImage(frameAvatar, 120 - (110 - deformation[index].X), 150 - (100 - deformation[index].Y));
            g.DrawImage(frame, 0, 0);

            return bitmap;
        }

        // Taken from https://github.com/sinus-x/rubbergoddess
        public async Task<string> SpankAsync(IUser target, IUser caller, bool harder)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
            if (caller == null)
                throw new ArgumentNullException(nameof(caller));

            var self = target.Equals(caller);
            var delayTime = harder ? 3 : 5;
            var gifName = CreateCachePath($"Spank{(self ? "_self" : "")}_{delayTime}_{target.Id}_{target.AvatarId ?? target.Discriminator}.gif");

            if (!File.Exists(gifName))
            {
                var profilePicture = await ProfilePictureService.GetProfilePictureAsync(target);
                using var gifWriter = new GcGifWriter(gifName);
                using var gcBitmap = new GcBitmap();

                if (self) profilePicture.RotateFlip(RotateFlipType.RotateNoneFlipX);

                var frames = GetBitmapsFromResources<SpankResources>();
                for (int i = 0; i < frames.Count; i++)
                {
                    var frame = frames[i];
                    var whipFrame = RenderSpankFrame(profilePicture, frame, i, harder);

                    if (self) whipFrame.RotateFlip(RotateFlipType.RotateNoneFlipX);

                    using var ms = new MemoryStream();
                    whipFrame.Save(ms, SysImgFormat.Png);

                    gcBitmap.Load(ms.ToArray());
                    gifWriter.AppendFrame(gcBitmap, disposalMethod: GifDisposalMethod.RestoreToBackgroundColor, delayTime: delayTime);
                }
            }

            return gifName;
        }

        static private Bitmap RenderSpankFrame(SysDrawImage profilePicture, Bitmap frame, int index, bool harder)
        {
            var deformation = new[] { 4, 2, 1, 0, 0, 0, 0, 3 };
            int deformationCoef = harder ? 7 : 2;

            var bitmap = new Bitmap(230, 150);
            bitmap.MakeTransparent();

            using var frameAvatar = profilePicture.ResizeImage(100 + (deformationCoef * deformation[index]), 100 + (deformationCoef * deformation[index]));

            using var g = Graphics.FromImage(bitmap);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            g.DrawImage(frame, new Point(10, 15));
            g.DrawImage(frameAvatar, new Point(80 - deformation[index], 10 - deformation[index]));

            return bitmap;
        }

        // Taken from https://github.com/Misha12/GrillBot
        public async Task<string> PeepoLoveAsync(IUser member, int fileUploadLimit)
        {
            var imageName = CreateCachePath($"Peepolove_{member.Id}_{member.AvatarId ?? member.Discriminator}.{(member.AvatarId.StartsWith("a_") ? "gif" : "png")}");

            if (!File.Exists(imageName))
            {
                var profilePictureData = await member.DownloadProfilePictureAsync(size: 256);
                using var memStream = new MemoryStream(profilePictureData);
                using var rawProfilePicture = SysDrawImage.FromStream(memStream);

                // Large animated profile pictures have problem with gif upload.
                if (Path.GetExtension(imageName) == ".gif" && profilePictureData.Length >= 2 * (fileUploadLimit / 3))
                    imageName = Path.ChangeExtension(imageName, ".png");

                if (Path.GetExtension(imageName) == ".gif")
                {
                    var frames = rawProfilePicture.SplitGifIntoFrames();

                    try
                    {
                        using var gifWriter = new GcGifWriter(imageName);
                        using var gcBitmap = new GcBitmap();

                        foreach (var userFrame in frames)
                        {
                            using var roundedUserFrame = userFrame.RoundImage();
                            using var frame = RenderPeepoloveFrame(roundedUserFrame);

                            using var ms = new MemoryStream();
                            frame.Save(ms, SysImgFormat.Png);

                            gcBitmap.Load(ms.ToArray());
                            gifWriter.AppendFrame(gcBitmap, disposalMethod: GifDisposalMethod.RestoreToBackgroundColor, delayTime: rawProfilePicture.CalculateGifDelay());
                        }
                    }
                    finally
                    {
                        frames.ForEach(o => o.Dispose());
                        frames.Clear();
                    }
                }
                else if (Path.GetExtension(imageName) == ".png")
                {
                    using var roundedProfileImage = rawProfilePicture.RoundImage();
                    using var profilePicture = roundedProfileImage.ResizeImage(256, 256);

                    using var frame = RenderPeepoloveFrame(profilePicture);
                    frame.Save(imageName, SysImgFormat.Png);
                }
            }

            return imageName;
        }

        static private SysDrawImage RenderPeepoloveFrame(SysDrawImage profilePicture)
        {
            using var body = new Bitmap(PeepoloveResources.body);
            using var graphics = Graphics.FromImage(body);

            graphics.RotateTransform(-0.4F);
            graphics.DrawImage(profilePicture, new Rectangle(5, 312, 180, 180));
            graphics.RotateTransform(0.4F);
            graphics.DrawImage(PeepoloveResources.hands, new Rectangle(0, 0, 512, 512));

            graphics.DrawImage(body, new Point(0, 0));
            return (body as SysDrawImage).CropImage(new Rectangle(0, 115, 512, 397));
        }
    }
}
