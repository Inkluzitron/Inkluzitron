using AnimatedGif;
using Discord;
using Discord.Commands;
using Inkluzitron.Extensions;
using Inkluzitron.Resources.Bonk;
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

        #region Common parts

        static private async Task<SysDrawImage> GetProfilePictureAsync(IUser user)
        {
            var profilePictureData = await user.DownloadProfilePictureAsync();
            using var memStream = new MemoryStream(profilePictureData);
            using var rawProfileImage = SysDrawImage.FromStream(memStream);
            using var roundedProfileImage = rawProfileImage.RoundImage();

            return roundedProfileImage.ResizeImage(100, 100);
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
