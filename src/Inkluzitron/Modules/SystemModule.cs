
using Discord.Commands;
using Inkluzitron.Extensions;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Inkluzitron.Modules
{
    [Name("System")]
    public class SystemModule : ModuleBase
    {
        [Command("uptime")]
        [Summary("Zjištění, jak dlouho bot běží.")]
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
