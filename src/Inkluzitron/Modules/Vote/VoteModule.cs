﻿using Discord.Commands;
using Inkluzitron.Extensions;
using Inkluzitron.Modules.Vote;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Inkluzitron.Modules
{
    [Name("Hlasování")]
    [Summary(ModuleSummary)]
    public class VoteModule : ModuleBase
    {
        public const string ModuleSummary = "Zprávu s příkazem hlasování je možné upravovat a měnit tak parametry hlasování i poté, co už začalo nebo skončilo.\r\n\r\n"
            + "**Ukázková zadání:**\r\n"
            + Example1 + "\r\n"
            + Example2 + "\r\n"
            + Example3 + "\r\n"
            + Example4 + "\r\n"
            + Example5;

        public const string Example1 = "$vote Otázka hlasování s pevným ukončením\r\n" +
            ":fire: Ohýnek\r\n" +
            ":droplet: Vodička\r\n" +
            "konec 1.4.2021\r\n";

        public const string Example2 = "$vote Otázka hlasování s relativním ukončením\r\n" +
            ":computer: Počítač\r\n" +
            ":book: Knížečka\r\n" +
            "konec za 10 minut\r\n";

        public const string Example3 = "$vote Otázka hlasování bez ukončení\r\n" +
            ":dollar: Peníze\r\n" +
            ":moneybag: Taky peníze, ale spousta\r\n";

        public const string Example4 = "$vote Fixed deadline in English\r\n" +
            ":fire: Hot\r\n" +
            ":droplet: Wet\r\n" +
            "deadline 1.4.2021\r\n";

        public const string Example5 = "$vote Relative deadline in English\r\n" +
            ":fire: Hot\r\n" +
            ":droplet: Wet\r\n" +
            "deadline in 10 minutes\r\n";

        public static readonly IReadOnlySet<string> VoteStartingCommands = typeof(VoteModule).ExtractCommandNames(nameof(VoteModule.Vote)).ToHashSet();

        private VoteService VoteService { get; }

        public VoteModule(VoteService voteService)
        {
            VoteService = voteService;
        }

        [Command("vote")]
        [Summary("Spustí hlasování.")]
        public async Task Vote([Remainder, Name("zadání")] string voteDefinitionText)
            => await VoteService.ProcessVoteCommandAsync(Context.Message, voteDefinitionText);
    }
}
