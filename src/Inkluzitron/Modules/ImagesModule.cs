using Discord;
using Discord.Commands;
using Inkluzitron.Extensions;
using Inkluzitron.Modules.Help;
using Inkluzitron.Services;
using System.Threading.Tasks;

namespace Inkluzitron.Modules
{
    [Name("Obrázkové příkazy")]
    [Summary("Některé příkazy mohou být závisle na výsledku BDSM testu. Proto pozor, na koho tyto příkazy používáte.")]
    public class ImagesModule : ModuleBase
    {
        private ImagesService ImagesService { get; }

        public ImagesModule(ImagesService imagesService)
        {
            ImagesService = imagesService;
        }

        [Command("bonk")]
        [Summary("Bonkne autora nebo zadaného uživatele.")]
        public async Task BonkAsync([Name("uživatel")]IUser member = null)
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

        [Group("whip")]
        [Summary("Použije bič na autora nebo zadaného uživatele.")]
        [DisableStandaloneHelpPage]
        public class WhipImageModule : ModuleBase
        {
            private ImagesService ImagesService { get; }
            private UserBdsmTraitsService UserBdsmTraits { get; }

            public WhipImageModule(ImagesService imagesService, UserBdsmTraitsService userBdsmTraits)
            {
                ImagesService = imagesService;
                UserBdsmTraits = userBdsmTraits;
            }

            private async Task WhipImplAsync(IUser target, bool showRollInfo)
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
                    // BDSM test check
                    var check = await UserBdsmTraits.CheckDomSubOperationAsync(Context.User, target);

                    if (!check.IsSuccessful)
                        target = Context.User;

                    if (showRollInfo)
                        messageText = check.ToString();

                    imagePath = await ImagesService.WhipAsync(target, Context.User);
                }

                await ReplyFileAsync(imagePath, messageText);
            }

            [Command("")]
            public Task WhipAsync([Name("uživatel")] IUser user = null)
                => WhipImplAsync(user, false);

            [Command("roll")]
            public Task WhipWithRollInfoAsync([Name("uživatel")] IUser user = null)
                => WhipImplAsync(user, true);
        }

        [Group("spank")]
        [Summary("Naplácá autorovi nebo zadanému uživateli.")]
        [DisableStandaloneHelpPage]
        public class SpankImageModule : ModuleBase
        {
            private ImagesService ImagesService { get; }
            private UserBdsmTraitsService UserBdsmTraits { get; }
            protected virtual bool Harder => false;

            public SpankImageModule(ImagesService imagesService, UserBdsmTraitsService userBdsmTraits)
            {
                ImagesService = imagesService;
                UserBdsmTraits = userBdsmTraits;
            }

            private async Task SpankImplAsync(IUser member, bool showRollInfo)
            {
                if (member == null)
                    member = Context.User;

                string messageText = null;
                string imagePath;

                if (member.Id == Context.Client.CurrentUser.Id)
                {
                    imagePath = await ImagesService.PeepoAngryAsync(Context.User, Context.Guild.CalculateFileUploadLimit());
                }
                else
                {
                    // BDSM test check
                    var check = await UserBdsmTraits.CheckDomSubOperationAsync(Context.User, member);

                    if (!check.IsSuccessful)
                        member = Context.User;

                    if (showRollInfo)
                        messageText = check.ToString();

                    imagePath = await ImagesService.SpankAsync(member, Context.User, Harder);
                }

                await ReplyFileAsync(imagePath, messageText);
            }

            [Command("")]
            public Task SpankAsync([Name("uživatel")] IUser user = null)
                => SpankImplAsync(user, false);

            [Command("roll")]
            public Task SpankWithRollInfoAsync([Name("uživatel")] IUser user = null)
                => SpankImplAsync(user, true);
        }

        [Group("spank-harder")]
        [Alias("harder-daddy")]
        [Summary("Naplácá s větší silou autorovi nebo zadanému uživateli.")]
        [DisableStandaloneHelpPage]
        public class SpankHarderImageModule : SpankImageModule
        {
            protected override bool Harder => true;

            public SpankHarderImageModule(ImagesService imagesService, UserBdsmTraitsService userBdsmTraits)
                : base(imagesService, userBdsmTraits)
            {
            }
        }
    }
}
