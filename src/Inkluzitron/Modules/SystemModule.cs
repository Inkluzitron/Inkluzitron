﻿
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
            var activeTime = process.TotalProcessorTime.FullTextFormat();

            await ReplyAsync($"Start proběhl **{process.StartTime:dd. MM. yyyy HH:mm:ss}**. To znamená, že běžím {uptime} (ale reálně jsem pracoval {activeTime}).");
        }

        [Command("version")]
        [Alias("verze")]
        [Summary("Vypíše aktuální verzi bota.")]
        public async Task GetVersionAsync()
        {
            var commitDate = DateTime.Parse(ThisAssembly.Git.CommitDate);
            await ReplyAsync($"Aktuální verze bota je `{ThisAssembly.Git.Commit}` (**{commitDate:dd. MM. yyyy HH:mm:ss}**)");
        }
    }
}
