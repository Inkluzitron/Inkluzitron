using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;

namespace Inkluzitron.Modules
{
  
  public class MockingModule : ModuleBase
  {
    private IConfiguration Config { set; get; }

    public MockingModule(IConfiguration config)
    {
      Config = config;
    }
    
    [Command("mock")]
    [Summary("Mockuje zadanou zprávu.")]
    public async Task MockAsync([Remainder] string message)
    {
      if (message.Length == 0)
      {
        await ReplyAsync("Chybí zpráva k mockování.");
        return;
      }

      var newString = "";
      var toUpper = false;
      foreach (var t in message)
      {
        if (t == ' ')
        {
          newString += t;
          continue;
        }

        newString += toUpper ? t.ToString().ToUpper() : t;
        toUpper = !toUpper;
      }

      await ReplyAsync(newString);
      await Context.Channel.SendFileAsync(Config["Spongebob"]);
      
    }
  }
}
