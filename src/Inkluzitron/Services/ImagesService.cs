using Discord;
using Discord.WebSocket;
using GrapeCity.Documents.Imaging;
using Inkluzitron.Extensions;
using Inkluzitron.Models;
using Inkluzitron.Models.Settings;
using Inkluzitron.Resources.Bonk;
using Inkluzitron.Resources.Miscellaneous;
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
        private DiscordSocketClient Client { get; }
        private ImageCacheSettings CacheSettings { get; }

        private DirectoryInfo AvatarCache { get; }
        private DirectoryInfo WhipCache { get; }
        private DirectoryInfo SpankCache { get; }
        private DirectoryInfo LoveCache { get; }
        private DirectoryInfo PatCache { get; }
        private DirectoryInfo BonkCache { get; }

        private DirectoryInfo InitializeCache(string directoryName)
        {
            var result = new DirectoryInfo(Path.Combine(CacheSettings.DirectoryPath, directoryName));
            result.Create();
            return result;
        }

        public ImagesService(DiscordSocketClient client, ImageCacheSettings cacheSettings)
        {
            Client = client;
            CacheSettings = cacheSettings;

            AvatarCache = InitializeCache("Avatars");
            WhipCache = InitializeCache("Whip");
            SpankCache = InitializeCache("Spank");
            LoveCache = InitializeCache("Love");
            PatCache = InitializeCache("Pat");
            BonkCache = InitializeCache("Bonk");
        }

        static private List<Bitmap> GetBitmapsFromResources<TResources>()
            => typeof(TResources).GetProperties()
                .Where(o => o.PropertyType == typeof(Bitmap))
                .Select(o => o.GetValue(null, null) as Bitmap)
                .Where(o => o != null)
                .ToList();

        static private string GetAvatarExtension(string avatarId)
            => avatarId.StartsWith("a_") ? "gif" : "png";

        static private AvatarImageWrapper CreateFallbackAvatarWrapper(Size? size = null)
        {
            var desiredSize = size ?? DefaultAvatarSize;

            using var roundedFallbackAvatar = MiscellaneousResources.FallbackAvatar.RoundImage();
            var resizedFallbackAvatar = roundedFallbackAvatar.ResizeImage(desiredSize.Width, desiredSize.Height);
            return new AvatarImageWrapper(resizedFallbackAvatar, 1, "png");
        }

        static private readonly Size DefaultAvatarSize = new Size(100, 100);

        public async Task<AvatarImageWrapper> GetAvatarAsync(SocketGuild guild, ulong userId, ushort discordSize = 128, Size? size = null)
        {
            if (guild == null)
                throw new ArgumentNullException(nameof(guild));

            IUser user = await guild.GetUserAsync(userId);
            user ??= await Client.Rest.GetUserAsync(userId);

            if (user == null)
                return CreateFallbackAvatarWrapper(size);
            else
                return await GetAvatarAsync(user, discordSize, size);
        }

        public async Task<AvatarImageWrapper> GetAvatarAsync(IUser user, ushort discordSize = 128, Size? size = null)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            var realSize = size ?? DefaultAvatarSize;
            var cacheObject = FileCacheObject.In(AvatarCache)
                .WithUnique(user.Id)
                .WithParam(user.AvatarId, discordSize, realSize.Width, realSize.Height)
                .Build();

            if (cacheObject.TryFind(out var filePath))
            {
                using var fileStream = File.OpenRead(filePath);
                return new AvatarImageWrapper(
                    SysDrawImage.FromStream(fileStream),
                    fileStream.Length,
                    Path.GetExtension(filePath)
                );
            }

            var profilePictureData = await user.DownloadProfilePictureAsync(size: discordSize);
            using var memStream = new MemoryStream(profilePictureData);
            using var rawProfileImage = SysDrawImage.FromStream(memStream);
            using var roundedProfileImage = rawProfileImage.RoundImage();

            var avatar = roundedProfileImage.ResizeImage(realSize.Width, realSize.Height);
            var extension = GetAvatarExtension(user.AvatarId);
            avatar.Save(cacheObject.GetPathForWriting(extension));
            return new AvatarImageWrapper(avatar, memStream.Length, extension);
        }

        // Taken from https://github.com/sinus-x/rubbergoddess
        public async Task<string> WhipAsync(IUser target, bool self)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            var cacheObject = FileCacheObject.In(WhipCache)
                .WithUnique(target.Id)
                .WithConditionalUnique("self", self)
                .WithParam(target.AvatarId ?? target.Discriminator)
                .Build();

            if (cacheObject.TryFind(out string filePath))
                return filePath;
            else
                filePath = cacheObject.GetPathForWriting("gif");

            using var avatar = await GetAvatarAsync(target);
            using var gifWriter = new GcGifWriter(filePath);
            using var gcBitmap = new GcBitmap();

            if (self)
                avatar.Image.RotateFlip(RotateFlipType.RotateNoneFlipX);

            var frames = GetBitmapsFromResources<WhipResources>();
            for (int i = 0; i < frames.Count; i++)
            {
                var frame = frames[i];
                using var whipFrame = RenderWhipFrame(avatar.Image, frame, i);

                if (self)
                    whipFrame.RotateFlip(RotateFlipType.RotateNoneFlipX);

                using var ms = new MemoryStream();
                whipFrame.Save(ms, SysImgFormat.Png);
                ms.Seek(0, SeekOrigin.Begin);

                gcBitmap.Load(ms);
                gifWriter.AppendFrame(gcBitmap, disposalMethod: GifDisposalMethod.RestoreToBackgroundColor);
            }

            return filePath;
        }

        static private Bitmap RenderWhipFrame(SysDrawImage avatar, Bitmap frame, int index)
        {
            var deformation = new[] { 0, 0, 0, 0, 0, 0, 0, 0, 2, 3, 5, 9, 6, 4, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var translation = new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 2, 3, 3, 3, 2, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            var bitmap = new Bitmap(250, 145);
            bitmap.MakeTransparent();

            using var frameAvatar = avatar.ResizeImage(100 - deformation[index], 100);

            using var g = Graphics.FromImage(bitmap);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            g.DrawImage(frameAvatar, 135 + deformation[index] + translation[index], 30);
            g.DrawImage(frame, 0, -10);

            return bitmap;
        }

        // Taken from https://github.com/sinus-x/rubbergoddess
        public async Task<string> BonkAsync(IUser target, bool self)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            var cacheObject = FileCacheObject.In(BonkCache)
                .WithUnique(target.Id)
                .WithConditionalUnique("self", self)
                .WithParam(target.AvatarId ?? target.Discriminator)
                .Build();

            if (cacheObject.TryFind(out string filePath))
                return filePath;
            else
                filePath = cacheObject.GetPathForWriting("gif");

            using var avatar = await GetAvatarAsync(target);
            using var gifWriter = new GcGifWriter(filePath);
            using var gcBitmap = new GcBitmap();

            if (self)
                avatar.Image.RotateFlip(RotateFlipType.RotateNoneFlipX);

            var frames = GetBitmapsFromResources<BonkResources>();
            for (int i = 0; i < frames.Count; i++)
            {
                var frame = frames[i];
                using var bonkFrame = RenderBonkFrame(avatar.Image, frame, i);

                if (self)
                    bonkFrame.RotateFlip(RotateFlipType.RotateNoneFlipX);

                using var ms = new MemoryStream();
                bonkFrame.Save(ms, SysImgFormat.Png);
                ms.Seek(0, SeekOrigin.Begin);

                gcBitmap.Load(ms);
                gifWriter.AppendFrame(gcBitmap, disposalMethod: GifDisposalMethod.RestoreToBackgroundColor, delayTime: 3);
            }

            return filePath;
        }

        static private Bitmap RenderBonkFrame(SysDrawImage avatar, Bitmap frame, int index)
        {
            var bitmap = new Bitmap(250, 170);
            bitmap.MakeTransparent();

            var deformation = new[] { 0, 0, 0, 0, 5, 20, 15, 5 };
            using var frameAvatar = avatar.ResizeImage(110, 100 - deformation[index]);

            using var g = Graphics.FromImage(bitmap);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            g.DrawImage(frameAvatar, 100, 60 + deformation[index]);
            g.DrawImage(frame, 0, 0);

            return bitmap;
        }

        // Taken from https://github.com/Misha12/GrillBot
        public async Task<string> PeepoAngryAsync(IUser target, int fileUploadLimit)
        {
            var cacheObject = FileCacheObject.In(BonkCache)
                .WithUnique(target.Id)
                .WithParam(target.AvatarId ?? target.Discriminator)
                .Build();

            if (cacheObject.TryFind(out var filePath))
                return filePath;

            using var avatar = await GetAvatarAsync(target, 64);
            var isAnimated = avatar.Extension == "gif";

            // Large animated profile pictures have problem with gif upload.
            if (isAnimated && avatar.FileSize >= 2 * (fileUploadLimit / 3))
                isAnimated = false;

            filePath = cacheObject.GetPathForWriting(isAnimated ? "gif" : "png");

            if (isAnimated)
            {
                var frames = avatar.Image.SplitGifIntoFrames();

                try
                {
                    using var gifWriter = new GcGifWriter(filePath);
                    using var gcBitmap = new GcBitmap();
                    var delayTime = avatar.Image.CalculateGifDelay();

                    foreach (var userFrame in frames)
                    {
                        using var roundedUserFrame = userFrame.RoundImage();
                        using var frame = RenderPeepoangryFrame(roundedUserFrame);

                        using var ms = new MemoryStream();
                        frame.Save(ms, SysImgFormat.Png);
                        ms.Seek(0, SeekOrigin.Begin);

                        gcBitmap.Load(ms);
                        gifWriter.AppendFrame(gcBitmap, disposalMethod: GifDisposalMethod.RestoreToBackgroundColor, delayTime: delayTime);
                    }
                }
                finally
                {
                    frames.ForEach(f => f.Dispose());
                    frames.Clear();
                }
            }
            else
            {
                using var frame = RenderPeepoangryFrame(avatar.Image);
                frame.Save(filePath, SysImgFormat.Png);
            }

            return filePath;
        }

        static private SysDrawImage RenderPeepoangryFrame(SysDrawImage avatar)
        {
            var body = new Bitmap(250, 105);
            using var graphics = Graphics.FromImage(body);

            graphics.DrawImage(avatar, new Rectangle(new Point(20, 10), new Size(64, 64)));
            graphics.DrawImage(PeepoangryResources.peepoangry, new Point(115, -5));

            return body;
        }

        // Taken from https://github.com/Toaster192/rubbergod
        public async Task<string> PatAsync(IUser target, bool self)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            var cacheObject = FileCacheObject.In(PatCache)
                .WithUnique(target.Id)
                .WithConditionalUnique("self", self)
                .WithParam(target.AvatarId ?? target.Discriminator)
                .Build();

            if (cacheObject.TryFind(out var filePath))
                return filePath;
            else
                filePath = cacheObject.GetPathForWriting(".gif");

            using var avatar = await GetAvatarAsync(target);
            using var gifWriter = new GcGifWriter(filePath);
            using var gcBitmap = new GcBitmap();

            if (self)
                avatar.Image.RotateFlip(RotateFlipType.RotateNoneFlipX);

            var frames = GetBitmapsFromResources<PatResources>();
            for (int i = 0; i < frames.Count; i++)
            {
                var frame = frames[i];
                using var bonkFrame = RenderPatFrame(avatar.Image, frame, i);

                if (self)
                    bonkFrame.RotateFlip(RotateFlipType.RotateNoneFlipX);

                using var ms = new MemoryStream();
                bonkFrame.Save(ms, SysImgFormat.Png);
                ms.Seek(0, SeekOrigin.Begin);

                gcBitmap.Load(ms);
                gifWriter.AppendFrame(gcBitmap, disposalMethod: GifDisposalMethod.RestoreToBackgroundColor, delayTime: 5);
            }

            return filePath;
        }

        static private Bitmap RenderPatFrame(SysDrawImage avatar, Bitmap frame, int index)
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

            using var frameAvatar = avatar.ResizeImage(100 - deformation[index].X, 100 - deformation[index].Y);

            using var g = Graphics.FromImage(bitmap);
            g.DrawImage(frameAvatar, 120 - (110 - deformation[index].X), 150 - (100 - deformation[index].Y));
            g.DrawImage(frame, 0, 0);

            return bitmap;
        }

        // Taken from https://github.com/sinus-x/rubbergoddess
        public async Task<string> SpankAsync(IUser target, bool self, bool harder)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            var delayTime = harder ? 3 : 5;

            var cacheObject = FileCacheObject.In(SpankCache)
                .WithUnique(target.Id)
                .WithConditionalUnique("self", self)
                .WithParam(delayTime)
                .WithParam(target.AvatarId ?? target.Discriminator)
                .Build();

            if (cacheObject.TryFind(out var filePath))
                return filePath;
            else
                filePath = cacheObject.GetPathForWriting("gif");

            using var avatar = await GetAvatarAsync(target);
            using var gifWriter = new GcGifWriter(filePath);
            using var gcBitmap = new GcBitmap();

            if (self)
                avatar.Image.RotateFlip(RotateFlipType.RotateNoneFlipX);

            var frames = GetBitmapsFromResources<SpankResources>();
            for (int i = 0; i < frames.Count; i++)
            {
                var frame = frames[i];
                using var whipFrame = RenderSpankFrame(avatar.Image, frame, i, harder);

                if (self)
                    whipFrame.RotateFlip(RotateFlipType.RotateNoneFlipX);

                using var ms = new MemoryStream();
                whipFrame.Save(ms, SysImgFormat.Png);
                ms.Seek(0, SeekOrigin.Begin);

                gcBitmap.Load(ms);
                gifWriter.AppendFrame(gcBitmap, disposalMethod: GifDisposalMethod.RestoreToBackgroundColor, delayTime: delayTime);
            }

            return filePath;
        }

        public Task<string> SpankGentleAsync(IUser target, bool self)
            => SpankAsync(target, self, false);

        public Task<string> SpankHarderAsync(IUser target, bool self)
            => SpankAsync(target, self, true);

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
        public async Task<string> PeepoLoveAsync(IUser target, int fileUploadLimit)
        {
            var cacheObject = FileCacheObject.In(LoveCache)
                .WithUnique(target.Id)
                .WithParam(target.AvatarId ?? target.Discriminator)
                .Build();

            if (cacheObject.TryFind(out var filePath))
                return filePath;

            using var avatar = await GetAvatarAsync(target, 256);
            var isAnimated = avatar.Extension == "gif";

            // Large animated profile pictures have problem with gif upload.
            if (isAnimated && avatar.FileSize >= 2 * (fileUploadLimit / 3))
                isAnimated = false;

            filePath = cacheObject.GetPathForWriting(isAnimated ? "gif" : "png");

            if (isAnimated)
            {
                var frames = avatar.Image.SplitGifIntoFrames();

                try
                {
                    using var gifWriter = new GcGifWriter(filePath);
                    using var gcBitmap = new GcBitmap();
                    var delay = avatar.Image.CalculateGifDelay();

                    foreach (var userFrame in frames)
                    {
                        using var roundedUserFrame = userFrame.RoundImage();
                        using var frame = RenderPeepoloveFrame(roundedUserFrame);

                        using var ms = new MemoryStream();
                        frame.Save(ms, SysImgFormat.Png);
                        ms.Seek(0, SeekOrigin.Begin);

                        gcBitmap.Load(ms);
                        gifWriter.AppendFrame(gcBitmap, disposalMethod: GifDisposalMethod.RestoreToBackgroundColor, delayTime: delay);
                    }
                }
                finally
                {
                    frames.ForEach(o => o.Dispose());
                    frames.Clear();
                }
            }
            else
            {
                using var frame = RenderPeepoloveFrame(avatar.Image);
                frame.Save(filePath, SysImgFormat.Png);
            }

            return filePath;
        }

        static private SysDrawImage RenderPeepoloveFrame(SysDrawImage avatar)
        {
            using var body = new Bitmap(PeepoloveResources.body);
            using var graphics = Graphics.FromImage(body);

            graphics.RotateTransform(-0.4F);
            graphics.DrawImage(avatar, new Rectangle(5, 312, 180, 180));
            graphics.RotateTransform(0.4F);
            graphics.DrawImage(PeepoloveResources.hands, new Rectangle(0, 0, 512, 512));

            graphics.DrawImage(body, new Point(0, 0));
            return (body as SysDrawImage).CropImage(new Rectangle(0, 115, 512, 397));
        }
    }
}
