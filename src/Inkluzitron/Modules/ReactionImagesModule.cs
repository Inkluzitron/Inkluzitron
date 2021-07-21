using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Inkluzitron.Extensions;
using Inkluzitron.Services;
using Microsoft.Extensions.Configuration;

namespace Inkluzitron.Modules
{
    [Name("Obrázkové příkazy")]
    [Summary("Za každý příkaz v této kategorii je možné napsat libovolnou zprávu.\nTyto příkazy jdou použít jako odpověď na zprávu, podobně jako $mock.")]
    public class ReactionImagesModule : ModuleBase
    {
        private ImagesService ImagesService { get; }
        private IConfiguration Config { get; }
        private UsersService UsersService { get; }

        public ReactionImagesModule(IConfiguration config, UsersService usersService, ImagesService imagesService)
        {
            Config = config;
            UsersService = usersService;
            ImagesService = imagesService;
        }

        [Command("hug")]
        [Summary("Hugne sebe nebo všechny uživatele, kteří jsou označení ve zprávě.")]
        public async Task HuggingAsync([Name("koho")] params IUser[] users)
        {
            if (users.Length == 0)
                users = new[] { Context.Message.ReferencedMessage?.Author ?? Context.User };

            foreach (var user in users)
            {
                var userName = await UsersService.GetDisplayNameAsync(user);
                await Context.Channel.SendMessageAsync(
                    $"{Config["Hugging"]} **{Format.Sanitize(userName)}**"
                );
            }
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
        public async Task BonkAsync([Name("uživatel")] IUser member = null, [Remainder][Name("")] string _ = null)
        {
            if (member == null)
                member = Context.Message.ReferencedMessage?.Author ?? Context.User;

            var gifName = await ImagesService.BonkAsync(member, Context.User.Equals(member));
            await ReplyFileAsync(gifName);
        }
    }
}
