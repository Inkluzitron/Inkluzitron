using Discord.Commands;
using System;
using System.Threading.Tasks;

namespace Inkluzitron.Modules
{
    public class RandomModule : ModuleBase
    {
        private Random Random { get; }

        public RandomModule(Random random)
        {
            Random = random;
        }

        [Command("pick")]
        [Summary("Vybere náhodnou možnost z možností.")]
        public async Task PickAsync(params string[] options)
        {
            if(options.Length == 0)
            {
                await ReplyAsync("Zadej nějaké možnosti.");
                return;
            }

            var selectedValue = options[Random.Next(0, options.Length - 1)];
            await ReplyAsync(selectedValue);
        }
    }
}
