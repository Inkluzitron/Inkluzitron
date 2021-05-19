
using Discord.Commands;
using Inkluzitron.Extensions;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Inkluzitron.Modules
{
    [Name("Systémové příkazy")]
    public class SystemModule : ModuleBase
    {
        [Command("uptime")]
        [Summary("Vypíše dobu od spuštění bota.")]
        public async Task UptimeAsync()
        {
            var process = Process.GetCurrentProcess();
            var uptime = (DateTime.Now - process.StartTime).FullTextFormat();
            var start = process.StartTime.ToString("dd. MM. yyyy HH:mm:ss");
            var activeTime = process.TotalProcessorTime.FullTextFormat();

            await ReplyAsync($"Start proběhl **{start}**. To znamená, že běžím {uptime} (ale reálně jsem pracoval {activeTime}).");
        }
    }
}
