using Discord;
using Discord.Commands;
using Inkluzitron.Extensions;
using Inkluzitron.Models;
using Inkluzitron.Services;
using System.Threading.Tasks;

namespace Inkluzitron.Modules
{
    [Name("Spicy obrázkové příkazy")]
    [Summary("Obrázkové příkazy, které jsou závislé na výsledcích BDSM testu. Pro jejich použití je třeba mít vyplněný BDSM test a obě strany musí udělit souhlas s použitím.\nPoužití je stejné jako u běžných obrázkových příkazů.")]
    public class SpicyReactionImagesModule : ModuleBase
    {
        private ImagesService ImagesService { get; }
        private UserBdsmTraitsService UserBdsmTraits { get; }
        private PointsService PointsService { get; }

        public SpicyReactionImagesModule(ImagesService imagesService, UserBdsmTraitsService userBdsmTraits,
            PointsService pointsService)
        {
            ImagesService = imagesService;
            UserBdsmTraits = userBdsmTraits;
            PointsService = pointsService;
        }

        [Command("whip")]
        [Summary("Použije bič na autora nebo zadaného uživatele.")]
        public Task WhipAsync([Name("uživatel")] IUser user = null, [Remainder][Name("")] string _ = null)
            => DomSubRolledImageAsync(user, false, ImagesService.WhipAsync);

        [Command("whip roll")]
        [Summary("Použije bič na autora nebo zadaného uživatele a vypíše vliv výsledků BDSM testu.")]
        public Task WhipWithRollInfoAsync([Name("uživatel")] IUser user = null, [Remainder][Name("")] string _ = null)
            => DomSubRolledImageAsync(user, true, ImagesService.WhipAsync);

        [Command("spank")]
        [Summary("Naplácá autorovi nebo zadanému uživateli.")]
        public Task SpankAsync([Name("uživatel")] IUser user = null, [Remainder][Name("")] string _ = null)
            => DomSubRolledImageAsync(user, false, ImagesService.SpankGentleAsync);

        [Command("spank roll")]
        [Summary("Naplácá autorovi nebo zadanému uživateli a vypíše vliv výsledků BDSM testu.")]
        public Task SpankWithRollInfoAsync([Name("uživatel")] IUser user = null, [Remainder][Name("")] string _ = null)
            => DomSubRolledImageAsync(user, true, ImagesService.SpankGentleAsync);

        [Command("spank harder")]
        [Alias("harder daddy", "spank-harder", "harder-daddy")]
        [Summary("Naplácá s větší silou autorovi nebo zadanému uživateli.")]
        public Task SpankHarderAsync([Name("uživatel")] IUser user = null, [Remainder][Name("")] string _ = null)
            => DomSubRolledImageAsync(user, false, ImagesService.SpankHarderAsync);

        [Command("spank harder roll")]
        [Alias("harder daddy roll", "spank-harder roll", "harder-daddy roll")]
        [Summary("Naplácá s větší silou autorovi nebo zadanému uživateli avypíše vliv výsledků BDSM testu.")]
        public Task SpankHarderWithRollInfoAsync([Name("uživatel")] IUser user = null, [Remainder][Name("")] string _ = null)
            => DomSubRolledImageAsync(user, true, ImagesService.SpankHarderAsync);

        private delegate Task<string> AsyncImageGenerator(IUser target, bool self);

        private async Task<RuntimeResult> DomSubRolledImageAsync(IUser target, bool showRollInfo, AsyncImageGenerator asyncImageGenerator)
        {
            if (target == null)
                target = Context.Message.ReferencedMessage?.Author ?? Context.User;

            if (await UserBdsmTraits.FindTestResultAsync(Context.User) == null)
            {
                // Whip-like commands can only by used by users who completed the BDSM test.
                return new CommandRedirectResult("bdsm");
            }

            string imagePath;
            string messageText = null;

            if (target.Id == Context.Client.CurrentUser.Id)
            {
                imagePath = await ImagesService.PeepoAngryAsync(Context.User, Context.Guild.CalculateFileUploadLimit());
            }
            else
            {
                var check = await UserBdsmTraits.CheckTraitOperationAsync(Context.User, target);

                if (check.Backfired)
                {
                    await PointsService.AddPointsAsync(Context.User, -check.PointsToSubtract);
                    await PointsService.AddPointsAsync(target, check.PointsToSubtract);
                    target = Context.User;
                    messageText = check.ToString();
                }
                else if (!check.CanProceedNormally) {
                    await ReplyAsync(check.ToString());
                    return null;
                }

                if (showRollInfo)
                    messageText = check.ToStringWithTraitInfluenceTable();

                imagePath = await asyncImageGenerator(target, Context.User.Equals(target));
            }

            await ReplyFileAsync(imagePath, messageText);
            return null;
        }
    }
}
