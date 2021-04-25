﻿using Discord;
using Discord.Commands;
using GrapeCity.Documents.Imaging;
using Inkluzitron.Extensions;
using Inkluzitron.Resources.Bonk;
using Inkluzitron.Resources.Pat;
using Inkluzitron.Resources.Peepoangry;
using Inkluzitron.Resources.Peepolove;
using Inkluzitron.Resources.Spank;
using Inkluzitron.Resources.Whip;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SysDrawImage = System.Drawing.Image;
using SysImgFormat = System.Drawing.Imaging.ImageFormat;

namespace Inkluzitron.Modules
{
    public class ImagesModule : ModuleBase
    {
        public ImagesModule()
        {
            if (!Directory.Exists("ImageCache"))
                Directory.CreateDirectory("ImageCache");
        }

        // Taken from https://github.com/sinus-x/rubbergoddess
        #region Bonk

        [Command("bonk")]
        public async Task BonkAsync(IUser member = null)
        {
            if (member == null) member = Context.User;

            if (member == Context.Guild.CurrentUser)
            {
                await PeepoangryAsync(Context.User);
                return;
            }

            var gifName = CreateCachePath($"bonk_{member.Id}_{member.AvatarId ?? member.Discriminator}.gif");

            if (!File.Exists(gifName))
            {
                var profilePicture = await GetProfilePictureAsync(member);
                using var gifWriter = new GcGifWriter(gifName);
                using var gcBitmap = new GcBitmap();

                var frames = GetBitmapsFromResources<BonkResources>();
                for (int i = 0; i < frames.Count; i++)
                {
                    var frame = frames[i];
                    using var bonkFrame = RenderBonkFrame(profilePicture, frame, i);

                    using var ms = new MemoryStream();
                    bonkFrame.Save(ms, SysImgFormat.Png);

                    gcBitmap.Load(ms.ToArray());
                    gifWriter.AppendFrame(gcBitmap, disposalMethod: GifDisposalMethod.RestoreToBackgroundColor, delayTime: 5);
                }
            }

            await ReplyFileAsync(gifName);
        }

        static private Bitmap RenderBonkFrame(SysDrawImage profilePicture, Bitmap frame, int index)
        {
            var bitmap = new Bitmap(250, 170);
            bitmap.MakeTransparent();

            var deformation = new[] { 0, 0, 0, 5, 10, 20, 15, 5 };
            using var frameAvatar = profilePicture.ResizeImage(110, 100 - deformation[index]);

            using var g = Graphics.FromImage(bitmap);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            g.DrawImage(frameAvatar, 100, 60 + deformation[index]);
            g.DrawImage(frame, 0, 0);

            return bitmap;
        }

        #endregion

        // Taken from https://github.com/Misha12/GrillBot
        #region Peepolove

        [Command("peepolove")]
        [Alias("love")]
        public async Task PeepoloveAsync(IUser member = null)
        {
            if (member == null) member = Context.User;
            var imageName = CreateCachePath($"Peepolove_{member.Id}_{member.AvatarId ?? member.Discriminator}.{(member.AvatarId.StartsWith("a_") ? "gif" : "png")}");

            if (!File.Exists(imageName))
            {
                var profilePictureData = await member.DownloadProfilePictureAsync(size: 256);
                using var memStream = new MemoryStream(profilePictureData);
                using var rawProfilePicture = SysDrawImage.FromStream(memStream);

                // Large animated profile pictures have problem with gif upload.
                if (Path.GetExtension(imageName) == ".gif" && profilePictureData.Length >= 2 * (Context.Guild.CalculateFileUploadLimit() / 3))
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

            await ReplyFileAsync(imageName);
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

        #endregion

        // Taken from https://github.com/Misha12/GrillBot
        #region Peepoangry

        [Command("peepoangry")]
        [Alias("angry")]
        public async Task PeepoangryAsync(IUser member = null)
        {
            if (member == null) member = Context.User;
            var imageName = CreateCachePath($"Peepoangry_{member.Id}_{member.AvatarId ?? member.Discriminator}.{(member.AvatarId.StartsWith("a_") ? "gif" : "png")}");

            if (!File.Exists(imageName))
            {
                var profilePictureData = await member.DownloadProfilePictureAsync(size: 64);
                using var memStream = new MemoryStream(profilePictureData);
                using var rawProfilePicture = SysDrawImage.FromStream(memStream);

                // Large animated profile pictures have problem with gif upload.
                if (Path.GetExtension(imageName) == ".gif" && profilePictureData.Length >= 2 * (Context.Guild.CalculateFileUploadLimit() / 3))
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

            await ReplyFileAsync(imageName);
        }

        static private SysDrawImage RenderPeepoangryFrame(SysDrawImage profilePicture)
        {
            var body = new Bitmap(250, 105);
            using var graphics = Graphics.FromImage(body);

            graphics.DrawImage(profilePicture, new Rectangle(new Point(20, 10), new Size(64, 64)));
            graphics.DrawImage(PeepoangryResources.peepoangry, new Point(115, -5));

            return body;
        }

        #endregion

        // Taken from https://github.com/sinus-x/rubbergoddess
        #region Whip

        [Command("whip")]
        public async Task WhipAsync(IUser member = null)
        {
            if (member == null) member = Context.User;

            if (member == Context.Guild.CurrentUser)
            {
                await PeepoangryAsync(Context.User);
                return;
            }

            var gifName = CreateCachePath($"Whip_{member.Id}_{member.AvatarId ?? member.Discriminator}.gif");

            if (!File.Exists(gifName))
            {
                var profilePicture = await GetProfilePictureAsync(member);
                using var gifWriter = new GcGifWriter(gifName);
                using var gcBitmap = new GcBitmap();

                var frames = GetBitmapsFromResources<WhipResources>();
                for (int i = 0; i < frames.Count; i++)
                {
                    var frame = frames[i];
                    var whipFrame = RenderWhipFrame(profilePicture, frame, i);

                    using var ms = new MemoryStream();
                    whipFrame.Save(ms, SysImgFormat.Png);

                    gcBitmap.Load(ms.ToArray());
                    gifWriter.AppendFrame(gcBitmap, disposalMethod: GifDisposalMethod.RestoreToBackgroundColor);
                }
            }

            await ReplyFileAsync(gifName);
        }

        static private Bitmap RenderWhipFrame(SysDrawImage profilePicture, Bitmap frame, int index)
        {
            var deformation = new[] { 0, 0, 0, 0, 0, 0, 0, 0, 2, 3, 5, 9, 6, 4, 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var translation = new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 2, 2, 3, 3, 3, 2, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

            var bitmap = new Bitmap(250, 150);
            bitmap.MakeTransparent();

            using var frameAvatar = profilePicture.ResizeImage(100 - deformation[index], 100);

            using var g = Graphics.FromImage(bitmap);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            g.DrawImage(frameAvatar, 135 + deformation[index] + translation[index], 25);
            g.DrawImage(frame, 0, 0);

            return bitmap;
        }

        #endregion

        // Taken from https://github.com/sinus-x/rubbergoddess
        #region Spank

        [Command("spank")]
        public async Task SpankAsync(IUser member = null)
        {
            await SpankAsync(member, false);
        }

        [Command("spank-harder")]
        public async Task SpankHarderAsync(IUser member = null)
        {
            await SpankAsync(member, true);
        }

        private async Task SpankAsync(IUser member, bool harder)
        {
            if (member == null) member = Context.User;

            if (member == Context.Guild.CurrentUser)
            {
                await PeepoangryAsync(Context.User);
                return;
            }

            int delayTime = harder ? 3 : 5;
            var gifName = CreateCachePath($"Spank_{delayTime}_{member.Id}_{member.AvatarId ?? member.Discriminator}.gif");

            if (!File.Exists(gifName))
            {
                var profilePicture = await GetProfilePictureAsync(member);
                using var gifWriter = new GcGifWriter(gifName);
                using var gcBitmap = new GcBitmap();

                var frames = GetBitmapsFromResources<SpankResources>();
                for (int i = 0; i < frames.Count; i++)
                {
                    var frame = frames[i];
                    var whipFrame = RenderSpankFrame(profilePicture, frame, i, harder);

                    using var ms = new MemoryStream();
                    whipFrame.Save(ms, SysImgFormat.Png);

                    gcBitmap.Load(ms.ToArray());
                    gifWriter.AppendFrame(gcBitmap, disposalMethod: GifDisposalMethod.RestoreToBackgroundColor, delayTime: delayTime);
                }
            }

            await ReplyFileAsync(gifName);
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

        #endregion

        // Taken from https://github.com/Toaster192/rubbergod
        #region Pat

        [Command("pat")]
        [Alias("pet")]
        public async Task PatAsync(IUser member = null)
        {
            if (member == null) member = Context.User;
            var gifName = CreateCachePath($"Pat_{member.Id}_{member.AvatarId ?? member.Discriminator}.gif");

            if (!File.Exists(gifName))
            {
                var profilePicture = await GetProfilePictureAsync(member);
                using var gifWriter = new GcGifWriter(gifName);
                using var gcBitmap = new GcBitmap();

                var frames = GetBitmapsFromResources<PatResources>();
                for (int i = 0; i < frames.Count; i++)
                {
                    var frame = frames[i];
                    using var bonkFrame = RenderPatFrame(profilePicture, frame, i);

                    using var ms = new MemoryStream();
                    bonkFrame.Save(ms, SysImgFormat.Png);

                    gcBitmap.Load(ms.ToArray());
                    gifWriter.AppendFrame(gcBitmap, disposalMethod: GifDisposalMethod.RestoreToBackgroundColor, delayTime: 5);
                }
            }

            await ReplyFileAsync(gifName);
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

        #endregion

        #region Common parts

        static private async Task<SysDrawImage> GetProfilePictureAsync(IUser user, ushort discordSize = 128, Size? size = null)
        {
            if (size == null) size = new Size(100, 100);

            var profilePictureData = await user.DownloadProfilePictureAsync(size: discordSize);
            using var memStream = new MemoryStream(profilePictureData);
            using var rawProfileImage = SysDrawImage.FromStream(memStream);
            using var roundedProfileImage = rawProfileImage.RoundImage();

            return roundedProfileImage.ResizeImage(size.Value.Width, size.Value.Height);
        }

        static private string CreateCachePath(string filename) => Path.Combine("ImageCache", filename);

        static private List<Bitmap> GetBitmapsFromResources<TResources>()
        {
            return typeof(TResources).GetProperties()
                .Where(o => o.PropertyType == typeof(Bitmap))
                .Select(o => o.GetValue(null, null) as Bitmap)
                .Where(o => o != null)
                .ToList();
        }

        #endregion
    }
}