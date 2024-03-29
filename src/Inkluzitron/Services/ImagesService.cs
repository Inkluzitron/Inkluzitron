﻿using Discord;
using Discord.WebSocket;
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
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Inkluzitron.Services
{
    public class ImagesService : IDisposable
    {
        static public readonly IMagickGeometry DefaultAvatarSize = new MagickGeometry(100, 100);
        static private readonly int AvatarFramesUpperLimit = 50;

        private DiscordSocketClient Client { get; }
        private FileCache Cache { get; }
        private MagickImageCollection WhipFrames { get; }
        private MagickImageCollection BonkFrames { get; }
        private MagickImageCollection PatFrames { get; }
        private MagickImageCollection SpankFrames { get; }
        private MagickImage PeepoloveBodyFrame { get; }
        private MagickImage PeepoloveHandsFrame { get; }
        private MagickImage PeepoangryFrame { get; }
        public IHttpClientFactory HttpClientFactory { get; }
        private ILogger<ImagesService> Logger { get; }


        public ImagesService(DiscordSocketClient client, FileCache fileCache, IHttpClientFactory httpClientFactory, ILogger<ImagesService> logger)
        {
            Client = client;
            Cache = fileCache;
            HttpClientFactory = httpClientFactory;
            Logger = logger;

            WhipFrames = GetFramesFromResources<WhipResources>();
            BonkFrames = GetFramesFromResources<BonkResources>();
            PatFrames = GetFramesFromResources<PatResources>();
            SpankFrames = GetFramesFromResources<SpankResources>();

            PeepoloveBodyFrame = new MagickImage(PeepoloveResources.body);
            PeepoloveHandsFrame = new MagickImage(PeepoloveResources.hands);
            PeepoangryFrame = new MagickImage(PeepoangryResources.peepoangry);
        }

        static private MagickImageCollection GetFramesFromResources<TResources>()
        {
            var collection = new MagickImageCollection();

            var bytesList = GetValuesOfByteArrayProperties<TResources>();
            foreach (var bytes in bytesList)
            {
                collection.Add(new MagickImage(bytes));
            }

            return collection;
        }

        static private List<byte[]> GetValuesOfByteArrayProperties<TResources>()
            => typeof(TResources).GetProperties()
                .Where(o => o.PropertyType == typeof(byte[]))
                .Select(o => o.GetValue(null, null) as byte[])
                .Where(o => o != null)
                .ToList();

        static private AvatarImageWrapper CreateFallbackAvatarWrapper(IMagickGeometry size = null)
        {
            if(size == null) size = DefaultAvatarSize;

            var fallbackAvatar = new MagickImageCollection(MiscellaneousResources.FallbackAvatar);
            foreach (var frame in fallbackAvatar)
            {
                frame.Resize(size.Width, size.Height);
            }

            return AvatarImageWrapper.FromImage(fallbackAvatar, 1, "png");
        }

        public async Task<AvatarImageWrapper> GetAvatarAsync(SocketGuild guild, ulong userId, ushort discordSize = 128, MagickGeometry size = null)
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

        public async Task<AvatarImageWrapper> GetAvatarAsync(IUser user, ushort discordSize = 128, MagickGeometry size = null)
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
                MagickImageCollection rawProfileImage;

                try
                {
                    var avatarUrl = user.GetUserOrDefaultAvatarUrl(ImageFormat.Auto, discordSize);
                    using var memStream = await HttpClientFactory.CreateClient().GetStreamAsync(avatarUrl);
                    rawProfileImage = new MagickImageCollection(memStream);
                }
                catch (Exception e)
                {
                    Logger.LogWarning(e, "Could not download avatar for user {0}", user.Id);
                    rawProfileImage = new MagickImageCollection(MiscellaneousResources.FallbackAvatar);
                }

                using (rawProfileImage)
                {
                    isAnimated = rawProfileImage.Count > 1;
                    extension = isAnimated.Value ? "gif" : "png";
                    var format = isAnimated.Value ? MagickFormat.Gif : MagickFormat.Png;

                    filePath = cacheObject.GetPathForWriting(extension);
                    rawProfileImage.Write(filePath, format);
                }
            }

            var fileInfo = new FileInfo(filePath);
            var image = new MagickImageCollection(filePath);
            // Limit maximum number of frames to optimize performance
            isAnimated = image.Count > 1 && image.Count <= AvatarFramesUpperLimit;
            extension ??= Path.GetExtension(filePath)[1..];

            if (isAnimated.Value)
                return AvatarImageWrapper.FromAnimatedImage(image, fileInfo.Length, extension);
            else
                return AvatarImageWrapper.FromImage(image, fileInfo.Length, extension);
        }

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
            var avatar = rawAvatar.Frames[0];
            avatar.Resize(100, 100);
            avatar.RoundImage();

            using var collection = new MagickImageCollection();

            if (self)
                avatar.Flop();

            var frames = WhipFrames;
            for (int i = 0; i < frames.Count; i++)
            {
                var frame = frames[i];
                var whipFrame = RenderWhipFrame(avatar, frame, i);

                if (self)
                    whipFrame.Flop();

                whipFrame.AnimationDelay = 2;
                whipFrame.GifDisposeMethod = GifDisposeMethod.Background;
                collection.Add(whipFrame);
            }

            collection.Coalesce();
            collection.Write(filePath, MagickFormat.Gif);

            return filePath;
        }

        static private IMagickImage<byte> RenderWhipFrame(IMagickImage<byte> avatar, IMagickImage<byte> sourceFrame, int index)
        {
            var deformation = new[] { 0, 0, 0, 0, 0, 0, 0, 0, 2, 3, 5, 9, 6, 4, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var translation = new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 2, 3, 3, 3, 2, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            var frame = new MagickImage(MagickColors.Transparent, 250, 145);

            using var frameAvatar = avatar.Clone();
            frameAvatar.InterpolativeResize(100 - deformation[index], 100, PixelInterpolateMethod.Bilinear);

            frame.Composite(sourceFrame, 0, -10, CompositeOperator.Over);
            frame.Composite(frameAvatar, 135 + deformation[index] + translation[index], 30, CompositeOperator.Over);

            return frame;
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
            var avatar = rawAvatar.Frames[0];
            avatar.Resize(100, 100);
            avatar.RoundImage();

            using var collection = new MagickImageCollection();

            if (self)
                avatar.Flop();

            var frames = BonkFrames;
            for (int i = 0; i < frames.Count; i++)
            {
                var frame = frames[i];
                var bonkFrame = RenderBonkFrame(avatar, frame, i);

                if (self)
                    bonkFrame.Flop();

                bonkFrame.AnimationDelay = 3;
                bonkFrame.GifDisposeMethod = GifDisposeMethod.Background;
                collection.Add(bonkFrame);
            }

            collection.Coalesce();
            collection.Write(filePath, MagickFormat.Gif);

            return filePath;
        }

        static private IMagickImage<byte> RenderBonkFrame(IMagickImage<byte> avatar, IMagickImage<byte> sourceFrame, int index)
        {
            var deformation = new[] { 0, 0, 0, 0, 5, 20, 15, 5 };

            var frame = new MagickImage(MagickColors.Transparent, 250, 170);

            using var frameAvatar = avatar.Clone();
            frameAvatar.InterpolativeResize(110, 100 - deformation[index], PixelInterpolateMethod.Bilinear);

            frame.Composite(frameAvatar, 100, 60 + deformation[index], CompositeOperator.Over);
            frame.Composite(sourceFrame, 0, 0, CompositeOperator.Over);

            return frame;
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
                using var collection = new MagickImageCollection();

                foreach (var avatarFrame in avatar.Frames)
                {
                    avatarFrame.Resize(75, 75);
                    avatarFrame.RoundImage();
                    var frame = RenderPeepoangryFrame(avatarFrame);

                    frame.AnimationDelay = avatarFrame.AnimationDelay;
                    frame.GifDisposeMethod = GifDisposeMethod.Background;
                    collection.Add(frame);
                }

                collection.Coalesce();
                collection.Write(filePath, MagickFormat.Gif);
            }
            else
            {
                var avatarFrame = avatar.Frames[0];
                avatarFrame.Resize(75, 75);
                avatarFrame.RoundImage();

                using var frame = RenderPeepoangryFrame(avatarFrame);
                frame.Write(filePath, MagickFormat.Png);
            }

            return filePath;
        }

        private IMagickImage<byte> RenderPeepoangryFrame(IMagickImage<byte> avatar)
        {
            var frame = new MagickImage(MagickColors.Transparent, 250, 105);

            frame.Composite(avatar, 20, 10, CompositeOperator.Over);
            frame.Composite(PeepoangryFrame, 115, -5, CompositeOperator.Over);

            return frame;
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
            var avatar = rawAvatar.Frames[0];
            avatar.Resize(100, 100);
            avatar.RoundImage();

            using var collection = new MagickImageCollection();

            if (self)
                avatar.Flop();

            var frames = PatFrames;
            for (int i = 0; i < frames.Count; i++)
            {
                var frame = frames[i];
                var patFrame = RenderPatFrame(avatar, frame, i);

                if (self)
                    patFrame.Flop();

                patFrame.AnimationDelay = 5;
                patFrame.GifDisposeMethod = GifDisposeMethod.Background;
                collection.Add(patFrame);
            }

            collection.Coalesce();
            collection.Write(filePath, MagickFormat.Gif);

            return filePath;
        }

        static private IMagickImage<byte> RenderPatFrame(IMagickImage<byte> avatar, IMagickImage<byte> sourceFrame, int index)
        {
            var deformation = new[]
            {
                new MagickGeometry(-1, 4, 0, 0),
                new MagickGeometry(-2, 3, 0, 0),
                new MagickGeometry(1, 1, 0, 0),
                new MagickGeometry(2, 1, 0, 0),
                new MagickGeometry(1, -4, 0, 0)
            };

            var frame = new MagickImage(MagickColors.Transparent, 130, 150);

            using var frameAvatar = avatar.Clone();
            frameAvatar.InterpolativeResize(100 - deformation[index].X, 100 - deformation[index].Y, PixelInterpolateMethod.Bilinear);

            frame.Composite(frameAvatar, 110 - frameAvatar.Width, 150 - frameAvatar.Height, CompositeOperator.Over);
            frame.Composite(sourceFrame, 0, 0, CompositeOperator.Over);

            return frame;
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
            var avatar = rawAvatar.Frames[0];
            avatar.Resize(100, 100);
            avatar.RoundImage();

            using var collection = new MagickImageCollection();

            if (self)
                avatar.Flop();

            var frames = SpankFrames;
            for (int i = 0; i < frames.Count; i++)
            {
                var frame = frames[i];
                var spankFrame = RenderSpankFrame(avatar, frame, i, harder);

                if (self)
                    spankFrame.Flop();

                spankFrame.AnimationDelay = delayTime;
                spankFrame.GifDisposeMethod = GifDisposeMethod.Background;
                collection.Add(spankFrame);
            }

            collection.Coalesce();
            collection.Write(filePath, MagickFormat.Gif);

            return filePath;
        }

        public Task<string> SpankGentleAsync(IUser target, bool self)
            => SpankAsync(target, self, false);

        public Task<string> SpankHarderAsync(IUser target, bool self)
            => SpankAsync(target, self, true);

        static private IMagickImage<byte> RenderSpankFrame(IMagickImage<byte> avatar, IMagickImage<byte> sourceFrame, int index, bool harder)
        {
            var deformation = new[] { 4, 2, 1, 0, 0, 0, 0, 3 };
            int deformationCoef = harder ? 7 : 2;

            var frame = new MagickImage(MagickColors.Transparent, 230, 150);

            using var frameAvatar = avatar.Clone();
            frameAvatar.InterpolativeResize(100 + (deformationCoef * deformation[index]), 100 + (deformationCoef * deformation[index]), PixelInterpolateMethod.Bilinear);

            frame.Composite(sourceFrame, 10, 15, CompositeOperator.Over);
            frame.Composite(frameAvatar, 80 - deformation[index], 10 - deformation[index], CompositeOperator.Over);

            return frame;
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

            using var avatar = await GetAvatarAsync(target);
            var isAnimated = avatar.Extension == "gif";

            // Large animated profile pictures have problem with gif upload.
            if (isAnimated && avatar.FileSize >= 2 * (fileUploadLimit / 3))
                isAnimated = false;

            filePath = cacheObject.GetPathForWriting(isAnimated ? "gif" : "png");

            if (isAnimated)
            {
                using var collection = new MagickImageCollection();

                foreach (var userFrame in avatar.Frames)
                {
                    userFrame.Resize(110, 110);
                    userFrame.RoundImage();
                    var frame = RenderPeepoloveFrame(userFrame);

                    frame.AnimationDelay = userFrame.AnimationDelay;
                    frame.GifDisposeMethod = GifDisposeMethod.Background;
                    collection.Add(frame);
                }

                collection.Coalesce();
                collection.Write(filePath, MagickFormat.Gif);
            }
            else
            {
                var avatarFrame = avatar.Frames[0];
                avatarFrame.Resize(110, 110);
                avatarFrame.RoundImage();

                using var frame = RenderPeepoloveFrame(avatarFrame);
                frame.Write(filePath, MagickFormat.Png);
            }

            return filePath;
        }

        private IMagickImage<byte> RenderPeepoloveFrame(IMagickImage<byte> avatar)
        {
            var body = PeepoloveBodyFrame.Clone();
            avatar.BackgroundColor = MagickColors.Transparent;
            avatar.Rotate(-15);

            body.Composite(avatar, -22, 15, CompositeOperator.Over);
            body.Composite(PeepoloveHandsFrame, 0, 0, CompositeOperator.Over);

            return body;
        }

        public void Dispose()
        {
            WhipFrames?.Dispose();
            BonkFrames?.Dispose();
            PatFrames?.Dispose();
            SpankFrames?.Dispose();
            PeepoloveBodyFrame?.Dispose();
            PeepoloveHandsFrame?.Dispose();
            PeepoangryFrame?.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}
