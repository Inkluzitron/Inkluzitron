using AnimatedGif;
using Discord;
using Discord.Commands;
using Inkluzitron.Extensions;
using Inkluzitron.Resources.Bonk;
using Inkluzitron.Resources.Peepolove;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SysDrawImage = System.Drawing.Image;

namespace Inkluzitron.Modules
{
    public class ImagesModule : ModuleBase
    {
        public ImagesModule()
        {
            if (!Directory.Exists("ImageCache"))
                Directory.CreateDirectory("ImageCache");
        }

        #region Bonk

        [Command("bonk")]
        [Summary("Praští uživatele baseballovou pálkou.")]
        public async Task BonkAsync(IUser member = null)
        {
            if (member == null) member = Context.User;
            var gifName = CreateCachePath($"bonk_{member.Id}_{member.AvatarId ?? member.Discriminator}.gif");

            if (!File.Exists(gifName))
            {
                var profilePicture = await GetProfilePictureAsync(member);
                using var gif = new AnimatedGifCreator(gifName);

                var frames = GetBitmapsFromResources<BonkResources>();
                for (int i = 0; i < frames.Count; i++)
                {
                    var frame = frames[i];
                    var bonkFrame = RenderBonkFrame(profilePicture, frame, i);

                    await gif.AddFrameAsync(bonkFrame, quality: GifQuality.Bit8);
                }
            }

            await ReplyFileAsync(gifName);
        }

        static private Bitmap RenderBonkFrame(SysDrawImage profilePicture, Bitmap frame, int index)
        {
            var bitmap = new Bitmap(200, 170);
            bitmap.MakeTransparent();

            var deformation = new[] { 0, 0, 0, 5, 10, 20, 15, 5 };
            using var frameAvatar = profilePicture.ResizeImage(110, 100 - deformation[index]);

            using var g = Graphics.FromImage(bitmap);
            g.DrawImage(frameAvatar, 80, 60 + deformation[index]);
            g.DrawImage(frame, 0, 0);

            return bitmap;
        }

        #endregion

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
                if (Path.GetExtension(imageName) == ".gif" && profilePictureData.Length >= (Context.Guild.CalculateFileUploadLimit() / 3))
                    imageName = Path.ChangeExtension(imageName, ".png");

                if (Path.GetExtension(imageName) == ".gif")
                {
                    var frames = rawProfilePicture.SplitGifIntoFrames();

                    try
                    {
                        using var gif = new AnimatedGifCreator(imageName, rawProfilePicture.CalculateGifDelay());
                        foreach (var userFrame in frames)
                        {
                            using var roundedUserFrame = userFrame.RoundImage();
                            using var frame = RenderPeepoloveFrame(roundedUserFrame);

                            await gif.AddFrameAsync(frame, quality: GifQuality.Bit8);
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
                    var profilePicture = roundedProfileImage.ResizeImage(256, 256);

                    using var frame = RenderPeepoloveFrame(profilePicture);
                    frame.Save(imageName, System.Drawing.Imaging.ImageFormat.Png);
                }
            }

            await ReplyFileAsync(imageName);
        }

        static private SysDrawImage RenderPeepoloveFrame(SysDrawImage profilePicture)
        {
            using var body = new Bitmap(PeepoloveResources.peepoBody);
            using var graphics = Graphics.FromImage(body);

            graphics.RotateTransform(-0.4F);
            graphics.DrawImage(profilePicture, new Rectangle(5, 312, 180, 180));
            graphics.RotateTransform(0.4F);
            graphics.DrawImage(PeepoloveResources.peepoHands, new Rectangle(0, 0, 512, 512));

            graphics.DrawImage(body, new Point(0, 0));
            return (body as SysDrawImage).CropImage(new Rectangle(0, 115, 512, 397));
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
