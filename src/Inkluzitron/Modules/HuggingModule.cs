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
        public async Task HuggingAsync([Name("koho")] params IUser[] users)
        {
            if (users.Length == 0)
            {
                var userName = await UsersService.GetDisplayNameAsync(Context.User);
                await Context.Channel.SendMessageAsync(
                    $"{Config["Hugging"]} **{Format.Sanitize(userName)}**"
                );
                return;
            }

            foreach (var user in users)
            {
                var userName = await UsersService.GetDisplayNameAsync(user);
                await Context.Channel.SendMessageAsync(
                    $"{Config["Hugging"]} **{Format.Sanitize(userName)}**"
                );
            }
        }
    }
}
