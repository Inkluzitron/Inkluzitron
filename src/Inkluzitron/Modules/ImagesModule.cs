using Discord;
using Discord.Commands;
using Inkluzitron.Extensions;
using Inkluzitron.Services;
using System;
using System.Threading.Tasks;

namespace Inkluzitron.Modules
{
    [Name("Obrázkové příkazy")]
    [Summary("Některé příkazy mohou být závisle na výsledku BDSM testu. Proto pozor, na koho tyto příkazy používáte.")]
    public class ImagesModule : ModuleBase
    {
        private ImagesService ImagesService { get; }
        private UserBdsmTraitsService UserBdsmTraits { get; }

        public ImagesModule(ImagesService imagesService, UserBdsmTraitsService userBdsmTraits)
        {
            ImagesService = imagesService;
            UserBdsmTraits = userBdsmTraits;
        }

        [Command("bonk")]
        [Summary("Bonkne autora nebo zadaného uživatele.")]
        public async Task BonkAsync([Name("uživatel")] IUser member = null)
        {
            if (member == null)
                member = Context.User;

            if (member.Id == Context.Client.CurrentUser.Id)
            {
                await PeepoAngryAsync(Context.User);
                return;
            }

            var gifName = await ImagesService.BonkAsync(member, Context.User);
            await ReplyFileAsync(gifName);
        }


        [Command("peepolove")]
        [Alias("love")]
        [Summary("Vytvoří peepa objímajícího autora nebo zadaného uživatele.")]
        public async Task PeepoLoveAsync([Name("uživatel")] IUser member = null)
        {
            if (member == null)
                member = Context.User;

            var imageName = await ImagesService.PeepoLoveAsync(member, Context.Guild.CalculateFileUploadLimit());
            await ReplyFileAsync(imageName);
        }


        [Command("peepoangry")]
        [Alias("angry")]
        [Summary("Vytvoří peepa, který je naštavený na autora nebo zadaného uživatele.")]
        public async Task PeepoAngryAsync([Name("uživatel")] IUser member = null)
        {
            if (member == null)
                member = Context.User;

            var imageName = await ImagesService.PeepoAngryAsync(member, Context.Guild.CalculateFileUploadLimit());
            await ReplyFileAsync(imageName);
        }


        [Command("pat")]
        [Alias("pet")]
        [Summary("Pohladí autora nebo zadaného uživatele.")]
        public async Task PatAsync([Name("uživatel")] IUser member = null)
        {
            if (member == null)
                member = Context.User;

            var gifName = await ImagesService.PatAsync(member, Context.User);
            await ReplyFileAsync(gifName);
        }

        [Command("whip")]
        [Summary("Použije bič na autora nebo zadaného uživatele.")]
        public Task WhipAsync([Name("uživatel")] IUser user = null)
            => DomSubRolledImageAsync(user, false, ImagesService.WhipAsync);

        [Command("whip roll")]
        [Summary("Použije bič na autora nebo zadaného uživatele a vypíše vliv výsledků BDSM testu.")]
        public Task WhipWithRollInfoAsync([Name("uživatel")] IUser user = null)
            => DomSubRolledImageAsync(user, true, ImagesService.WhipAsync);

        [Command("spank")]
        [Summary("Naplácá autorovi nebo zadanému uživateli.")]
        public Task SpankAsync([Name("uživatel")] IUser user = null)
            => DomSubRolledImageAsync(user, false, ImagesService.SpankGentleAsync);

        [Command("spank roll")]
        [Summary("Naplácá autorovi nebo zadanému uživateli a vypíše vliv výsledků BDSM testu.")]
        public Task SpankWithRollInfoAsync([Name("uživatel")] IUser user = null)
            => DomSubRolledImageAsync(user, true, ImagesService.SpankGentleAsync);

        [Command("spank-harder")]
        [Alias("harder-daddy")]
        [Summary("Naplácá s větší silou autorovi nebo zadanému uživateli.")]
        public Task SpankHarderAsync([Name("uživatel")] IUser user = null)
            => DomSubRolledImageAsync(user, false, ImagesService.SpankHarderAsync);

        [Command("spank-harder roll")]
        [Alias("harder-daddy roll")]
        [Summary("Naplácá s větší silou autorovi nebo zadanému uživateli avypíše vliv výsledků BDSM testu.")]
        public Task SpankHarderWithRollInfoAsync([Name("uživatel")] IUser user = null)
            => DomSubRolledImageAsync(user, true, ImagesService.SpankHarderAsync);

        private async Task DomSubRolledImageAsync(IUser target, bool showRollInfo, Func<IUser, IUser, Task<string>> imageFactory)
        {
            if (target == null)
                target = Context.User;

            string imagePath;
            string messageText = null;

            if (target.Id == Context.Client.CurrentUser.Id)
            {
                imagePath = await ImagesService.PeepoAngryAsync(Context.User, Context.Guild.CalculateFileUploadLimit());
            }
            else
            {
                var check = await UserBdsmTraits.CheckDomSubOperationAsync(Context.User, target);

                if (!check.IsSuccessful)
                    target = Context.User;

                if (showRollInfo)
                    messageText = check.ToString();

                imagePath = await imageFactory(target, Context.User);
            }

            await ReplyFileAsync(imagePath, messageText);
        }
    }
}
