using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Inkluzitron.Services;
using Microsoft.Extensions.Configuration;

namespace Inkluzitron.Modules
{
    [Name("Hugování")]
    public class HuggingModule : ModuleBase
    {
        private IConfiguration Config { get; }
        private UsersService UsersService { get; }

        public HuggingModule(IConfiguration config, UsersService usersService)
        {
            Config = config;
            UsersService = usersService;
        }

        [Command("hug")]
        [Summary("Hugne sebe nebo všechny uživatele, kteří jsou označení ve zprávě.")]
        public async Task HuggingAsync([Name("zpráva")] params IUser[] user)
        {
            if (user.Length == 0)
            {
                var userName = await UsersService.GetDisplayNameAsync(Context.User);
                await Context.Channel.SendMessageAsync(
                    $"{Config["Hugging"]} **{Format.Sanitize(userName)}**"
                );
                return;
            }

            foreach (var u in user)
            {
                var userName = await UsersService.GetDisplayNameAsync(u);
                await Context.Channel.SendMessageAsync(
                    $"{Config["Hugging"]} **{Format.Sanitize(userName)}**"
                );
            }
        }
    }
}
