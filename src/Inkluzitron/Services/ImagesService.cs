using Discord;
using Discord.WebSocket;
using GrapeCity.Documents.Imaging;
using ImageMagick;
using Inkluzitron.Extensions;
using Inkluzitron.Models;
using Inkluzitron.Resources.Bonk;
using Inkluzitron.Resources.Miscellaneous;
using Inkluzitron.Resources.Pat;
using Inkluzitron.Resources.Peepoangry;
using Inkluzitron.Resources.Peepolove;
using Inkluzitron.Resources.Spank;
using Inkluzitron.Resources.Whip;
using Inkluzitron.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Inkluzitron.Services
{
    public class ImagesService
    {
        static public readonly Size DefaultAvatarSize = new(100, 100);

        private DiscordSocketClient Client { get; }
        private FileCache Cache { get; }
        public IHttpClientFactory HttpClientFactory { get; }

        public ImagesService(DiscordSocketClient client, FileCache fileCache, IHttpClientFactory httpClientFactory)
        {
            Client = client;
            Cache = fileCache;
            HttpClientFactory = httpClientFactory;
        }

        static private List<Bitmap> GetBitmapsFromResources<TResources>()
            => typeof(TResources).GetProperties()
                .Where(o => o.PropertyType == typeof(Bitmap))
                .Select(o => o.GetValue(null, null) as Bitmap)
                .Where(o => o != null)
                .ToList();

        static private AvatarImageWrapper CreateFallbackAvatarWrapper(Size? size = null)
        {
            var desiredSize = size ?? DefaultAvatarSize;

            using var roundedFallbackAvatar = MiscellaneousResources.FallbackAvatar;//.RoundImage(); TODO
            var resizedFallbackAvatar = roundedFallbackAvatar;//.ResizeImage(desiredSize.Width, desiredSize.Height); TODO
            using var stream = new MemoryStream();
            // TODO resizedFallbackAvatar.Save(stream);
            return AvatarImageWrapper.FromImage(new MagickImageCollection(stream), 1, "png");
        }

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
            var cacheObject = Cache.WithCategory("Avatars")
                .WithUnique(user.Id)
                .WithParam(user.AvatarId, discordSize, realSize.Width, realSize.Height)
                .Build();

            bool? isAnimated = null;
            string extension = null;

            if (!cacheObject.TryFind(out var filePath))
            {
                var avatarUrl = user.GetUserOrDefaultAvatarUrl(Discord.ImageFormat.Auto, discordSize);
                using var memStream = await HttpClientFactory.CreateClient().GetStreamAsync(avatarUrl);
                using var rawProfileImage = new MagickImageCollection(memStream);

                isAnimated = rawProfileImage.Count > 1;
                extension = isAnimated.Value ? "gif" : "png";
                var format = isAnimated.Value ? MagickFormat.Gif : MagickFormat.Png;

                filePath = cacheObject.GetPathForWriting(extension);
                rawProfileImage.Write(filePath, format);
            }

            var fileInfo = new FileInfo(filePath);
            var image = new MagickImageCollection(filePath);
            isAnimated ??= image.Count > 1;
            extension ??= Path.GetExtension(filePath)[1..];

            if (isAnimated.Value)
                return AvatarImageWrapper.FromAnimatedImage(image, fileInfo.Length, extension);
            else
                return AvatarImageWrapper.FromImage(image, fileInfo.Length, extension);
        }
        /*
        // Taken from https://github.com/sinus-x/rubbergoddess
        public async Task<string> WhipAsync(IUser target, bool self)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            var cacheObject = Cache.WithCategory("Whip")
                .WithUnique(target.Id)
                .WithConditionalUnique("self", self)
                .WithParam(target.AvatarId ?? target.Discriminator)
                .Build();

            if (cacheObject.TryFind(out string filePath))
                return filePath;
            else
                filePath = cacheObject.GetPathForWriting("gif");

            using var rawAvatar = await GetAvatarAsync(target);
            using var roundedAvatar = rawAvatar.Frames[0].RoundImage();
            using var avatar = roundedAvatar.ResizeImage(100, 100);

            using var gifWriter = new GcGifWriter(filePath);
            using var gcBitmap = new GcBitmap();

            if (self)
                avatar.RotateFlip(RotateFlipType.RotateNoneFlipX);

            var frames = GetBitmapsFromResources<WhipResources>();
            for (int i = 0; i < frames.Count; i++)
            {
                var frame = frames[i];
                using var whipFrame = RenderWhipFrame(avatar, frame, i);

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

            var cacheObject = Cache.WithCategory("Bonk")
                .WithUnique(target.Id)
                .WithConditionalUnique("self", self)
                .WithParam(target.AvatarId ?? target.Discriminator)
                .Build();

            if (cacheObject.TryFind(out string filePath))
                return filePath;
            else
                filePath = cacheObject.GetPathForWriting("gif");

            using var rawAvatar = await GetAvatarAsync(target);
            using var roundedAvatar = rawAvatar.Frames[0].RoundImage();
            using var avatar = roundedAvatar.ResizeImage(100, 100);

            using var gifWriter = new GcGifWriter(filePath);
            using var gcBitmap = new GcBitmap();

            if (self)
                avatar.RotateFlip(RotateFlipType.RotateNoneFlipX);

            var frames = GetBitmapsFromResources<BonkResources>();
            for (int i = 0; i < frames.Count; i++)
            {
                var frame = frames[i];
                using var bonkFrame = RenderBonkFrame(avatar, frame, i);

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
            var cacheObject = Cache.WithCategory("Angry")
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
                using var gifWriter = new GcGifWriter(filePath);
                using var gcBitmap = new GcBitmap();

                foreach (var userFrame in avatar.Frames)
                {
                    using var roundedUserFrame = userFrame.RoundImage();
                    using var frame = RenderPeepoangryFrame(roundedUserFrame);

                    using var ms = new MemoryStream();
                    frame.Save(ms, SysImgFormat.Png);
                    ms.Seek(0, SeekOrigin.Begin);

                    gcBitmap.Load(ms);
                    gifWriter.AppendFrame(gcBitmap, disposalMethod: GifDisposalMethod.RestoreToBackgroundColor, delayTime: avatar.GifDelay.Value);
                }
            }
            else
            {
                using var roundedAvatar = avatar.Frames[0].RoundImage();
                using var finalAvatar = roundedAvatar.ResizeImage(100, 100);

                using var frame = RenderPeepoangryFrame(finalAvatar);
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

            var cacheObject = Cache.WithCategory("Pat")
                .WithUnique(target.Id)
                .WithConditionalUnique("self", self)
                .WithParam(target.AvatarId ?? target.Discriminator)
                .Build();

            if (cacheObject.TryFind(out var filePath))
                return filePath;
            else
                filePath = cacheObject.GetPathForWriting(".gif");

            using var rawAvatar = await GetAvatarAsync(target);
            using var roundedAvatar = rawAvatar.Frames[0].RoundImage();
            using var avatar = roundedAvatar.ResizeImage(100, 100);

            using var gifWriter = new GcGifWriter(filePath);
            using var gcBitmap = new GcBitmap();

            if (self)
                avatar.RotateFlip(RotateFlipType.RotateNoneFlipX);

            var frames = GetBitmapsFromResources<PatResources>();
            for (int i = 0; i < frames.Count; i++)
            {
                var frame = frames[i];
                using var bonkFrame = RenderPatFrame(avatar, frame, i);

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

            var cacheObject = Cache.WithCategory("Spank")
                .WithUnique(target.Id)
                .WithConditionalUnique("self", self)
                .WithParam(delayTime)
                .WithParam(target.AvatarId ?? target.Discriminator)
                .Build();

            if (cacheObject.TryFind(out var filePath))
                return filePath;
            else
                filePath = cacheObject.GetPathForWriting("gif");

            using var rawAvatar = await GetAvatarAsync(target);
            using var roundedAvatar = rawAvatar.Frames[0].RoundImage();
            using var avatar = roundedAvatar.ResizeImage(100, 100);

            using var gifWriter = new GcGifWriter(filePath);
            using var gcBitmap = new GcBitmap();

            if (self)
                avatar.RotateFlip(RotateFlipType.RotateNoneFlipX);

            var frames = GetBitmapsFromResources<SpankResources>();
            for (int i = 0; i < frames.Count; i++)
            {
                var frame = frames[i];
                using var whipFrame = RenderSpankFrame(avatar, frame, i, harder);

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
            var cacheObject = Cache.WithCategory("Love")
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
                using var gifWriter = new GcGifWriter(filePath);
                using var gcBitmap = new GcBitmap();

                foreach (var userFrame in avatar.Frames)
                {
                    using var roundedUserFrame = userFrame.RoundImage();
                    using var frame = RenderPeepoloveFrame(roundedUserFrame);

                    using var ms = new MemoryStream();
                    frame.Save(ms, SysImgFormat.Png);
                    ms.Seek(0, SeekOrigin.Begin);

                    gcBitmap.Load(ms);
                    gifWriter.AppendFrame(gcBitmap, disposalMethod: GifDisposalMethod.RestoreToBackgroundColor, delayTime: avatar.GifDelay.Value);
                }
            }
            else
            {
                using var roundedAvatar = avatar.Frames[0].RoundImage();
                using var finalAvatar = roundedAvatar.ResizeImage(100, 100);

                using var frame = RenderPeepoloveFrame(finalAvatar);
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
        */
    }
}
