using System;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;

namespace Inkluzitron.Modules
{
  [Name("Hugování")]
  public class HuggingModule : ModuleBase
  {
    private IConfiguration Config { get; }

    public HuggingModule(IConfiguration config)
    {
      Config = config;
    }

    [Command("hug")]
    [Summary("Hugne uživatele, který vyvolal command, nebo uživatele, kterého specifikuje ve své zprávě.")]
    public async Task HuggingAsync([Remainder] string message = null)
    {
      var taggedList = Context.Message.MentionedUsers;
      var userName = "";

      // hug user sending command if no message is present
      if (message == null && taggedList.Count == 0)
      {
        if (Context.User is SocketGuildUser user)
        {
          userName = user.Nickname ?? user.Username;
          await Context.Channel.SendMessageAsync(
            $"{Config["Hugging"]} **{userName}**"
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

          userName = usr.Nickname ?? usr.Username;
          await Context.Channel.SendMessageAsync(
            $"{Config["Hugging"]} **{userName}**"
          );
        }

        return;
      }

      // iterate over users and find if someone has same name or nickname as message
      var foundSomeone = false;
      foreach (var user in Context.Guild.Users)
      {
        if (message == user.Nickname || message == user.Username)
        {
          foundSomeone = true;
          await Context.Channel.SendMessageAsync(
            $"{Config["Hugging"]} **{message}**"
          );
        }
      }

      if (foundSomeone) return;
      await Context.Channel.SendMessageAsync(
        $"Nastala politování hodná situace, nenašel jsem nikoho na ohugování... {Config["PepeHands"]}"
      );
    }
  }
}
