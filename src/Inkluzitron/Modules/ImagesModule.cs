using Discord;
using Discord.Commands;
using Inkluzitron.Extensions;
using Inkluzitron.Models;
using Inkluzitron.Services;
using System.Threading.Tasks;

namespace Inkluzitron.Modules
{
    [Name("Obrázkové příkazy")]
    [Summary("Za každý příkaz v této kategorii je možné napsat libovolnou zprávu.\nTyto příkazy jdou použít jako odpověď na zprávu, podobně jako $mock.\nTaké pozor, na koho příkazy používáte. Některé spicy příkazy jsou závislé na výsledku BDSM testu.")]
    public class ImagesModule : ModuleBase
    {
        private ImagesService ImagesService { get; }
        private UserBdsmTraitsService UserBdsmTraits { get; }
        private PointsService PointsService { get; }

        public ImagesModule(ImagesService imagesService, UserBdsmTraitsService userBdsmTraits,
            PointsService pointsService)
        {
            ImagesService = imagesService;
            UserBdsmTraits = userBdsmTraits;
            PointsService = pointsService;
        }

        [Command("peepolove")]
        [Alias("peepo love", "love")]
        [Summary("Vytvoří obrázek peepa objímajícího autora nebo zadaného uživatele.")]
        public async Task PeepoLoveAsync([Name("uživatel")] IUser member = null, [Remainder][Name("")] string _ = null)
        {
            if (member == null)
                member = Context.Message.ReferencedMessage?.Author ?? Context.User;

            var imageName = await ImagesService.PeepoLoveAsync(member, Context.Guild.CalculateFileUploadLimit());
            await ReplyFileAsync(imageName);
        }

        [Command("peepoangry")]
        [Alias("peepo angry", "angry")]
        [Summary("Vytvoří obrázek peepa, který je naštvaný na autora nebo zadaného uživatele.")]
        public async Task PeepoAngryAsync([Name("uživatel")] IUser member = null, [Remainder][Name("")] string _ = null)
        {
            if (member == null)
                member = Context.Message.ReferencedMessage?.Author ?? Context.User;

            var imageName = await ImagesService.PeepoAngryAsync(member, Context.Guild.CalculateFileUploadLimit());
            await ReplyFileAsync(imageName);
        }

        [Command("pat")]
        [Alias("pet")]
        [Summary("Pohladí autora nebo zadaného uživatele.")]
        public async Task PatAsync([Name("uživatel")] IUser member = null, [Remainder][Name("")] string _ = null)
        {
            if (member == null)
                member = Context.Message.ReferencedMessage?.Author ?? Context.User;

            var gifName = await ImagesService.PatAsync(member, Context.User.Equals(member));
            await ReplyFileAsync(gifName);
        }

        [Command("bonk")]
        [Summary("Bonkne autora nebo zadaného uživatele.")]
        public Task BonkAsync([Name("uživatel")] IUser user = null, [Remainder][Name("")] string _ = null)
            => DomSubRolledImageAsync(user, false, ImagesService.BonkAsync);

        [Command("bonk roll")]
        [Summary("Bonkne autora nebo zadaného uživatele a vypíše vliv výsledků BDSM testu.")]
        public Task BonkWithRollInfoAsync([Name("uživatel")] IUser user = null, [Remainder][Name("")] string _ = null)
            => DomSubRolledImageAsync(user, true, ImagesService.BonkAsync);

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

            if (!await UserBdsmTraits.TestExists(Context.User))
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
                var check = await UserBdsmTraits.CheckDomSubOperationAsync(Context.User, target);

                if (check.Backfired)
                {
                    await PointsService.AddPointsAsync(Context.User, -check.PointsToSubtract);
                    await PointsService.AddPointsAsync(target, check.PointsToSubtract);
                    target = Context.User;
                }
                else if (!check.CanProceedNormally) {
                    await ReplyAsync(check.ToString());
                    return null;
                }

                if (showRollInfo)
                    messageText = check.ToString();

                imagePath = await asyncImageGenerator(target, Context.User.Equals(target));
            }

            await ReplyFileAsync(imagePath, messageText);
            return null;
        }
    }
}
