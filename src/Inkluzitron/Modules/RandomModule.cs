using Discord.Commands;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Inkluzitron.Modules
{
    [Name("Random")]
    public class RandomModule : ModuleBase
    {
        [Command("pick")]
        [Summary("Vybere náhodně jednu ze zadaných možností.")]
        public async Task PickAsync(
            [Name("možnost1")] string option,
            [Name("možnost2 ...")] params string[] options)
        {
            options = options.Append(option).ToArray();

            var selectedValue = options[ThreadSafeRandom.Next(options.Length)];
            await ReplyAsync(selectedValue);
        }

        [Command("roll")]
        [Summary("Vrátí náhodné číslo od 0 do zadaného čísla.")]
        public Task RollAsync([Name("do")] int to)
            => RollAsync(0, to);

        [Command("roll")]
        [Summary("Vrátí náhodné číslo ze zadaného rozsahu.")]
        public async Task RollAsync(
            [Name("od")] int from,
            [Name("do")] int to)
        {
            if (from > to)
            {
                var temp = from;
                from = to;
                to = temp;
            }

            var selectedValue = ThreadSafeRandom.Next(from, to + 1);
            await ReplyAsync(selectedValue.ToString());
        }
    }
}
