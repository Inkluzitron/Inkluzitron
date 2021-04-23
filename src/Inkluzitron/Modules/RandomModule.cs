using Discord.Commands;
using System;
using System.Linq;
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
        public async Task PickAsync(
            [Summary("možnost")] string option,
            [Summary("možnost ...")] params string[] options)
        {
            options = options.Append(option).ToArray();
            
            var selectedValue = options[Random.Next(options.Length)];
            await ReplyAsync(selectedValue);
        }

        [Command("roll")]
        [Summary("Vrátí náhodné číslo ze zadaného rozsahu.")]
        public async Task RollAsync(
            [Summary("od")]int from,
            [Summary("do")]int to=0
        )
        {
            if(from > to)
            {
                var temp = from;
                from = to;
                to = temp;
            }

            var selectedValue = Random.Next(from, to+1);
            await ReplyAsync(selectedValue.ToString());
        }
    }
}
