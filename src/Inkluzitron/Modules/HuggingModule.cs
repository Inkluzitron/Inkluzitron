using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
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
        public async Task HuggingAsync([Remainder][Name("zpráva")] string message = null)
        {
            var taggedList = Context.Message.MentionedUsers;
            string userName;

            // hug user sending command if no message is present
            if (message == null && taggedList.Count == 0)
            {
                if (Context.User is SocketGuildUser user)
                {
                    userName = await UsersService.GetDisplayNameAsync(user);
                    await Context.Channel.SendMessageAsync(
                      $"{Config["Hugging"]} **{Format.Sanitize(userName)}**"
                    );
                }

                return;
            }

            // check if someone is tagged
            if (taggedList.Count != 0)
            {
                foreach (var user in taggedList)
                {
                    if (user is not SocketGuildUser usr) continue;

                    userName = await UsersService.GetDisplayNameAsync(usr);
                    await Context.Channel.SendMessageAsync(
                      $"{Config["Hugging"]} **{Format.Sanitize(userName)}**"
                    );
                }

                return;
            }

            // iterate over users and find if someone has same name, nickname or ID as message
            foreach (var user in Context.Guild.Users)
            {
                userName = await UsersService.GetDisplayNameAsync(user);
                if (message == userName || message == user.Id.ToString())
                {
                    await Context.Channel.SendMessageAsync(
                      $"{Config["Hugging"]} **{Format.Sanitize(userName)}**"
                    );
                    return;
                }
            }

            await Context.Channel.SendMessageAsync(
              $"Nastala politováníhodná situace, nenašel jsem nikoho na ohugování... {Config["PepeHands"]}"
            );
        }
    }
}
