﻿using Discord;
using Inkluzitron.Extensions;
using Microsoft.Extensions.Configuration;

namespace Inkluzitron.Models.Settings
{
    public class VoteSettings
    {
        static private readonly string SettingsSectionName = "VoteModule";

        public ulong MuteRole { get; set; }
        public IEmote MuteReactionFor { get; set; }
        public IEmote MuteReactionAgainst { get; set; }
        public int MuteVoteMinVotes { get; set; }
        public int MuteVoteMaxVotes { get; set; }

        public VoteSettings(IConfiguration config)
        {
            var section = config.GetSection(SettingsSectionName);
            MuteRole = section.GetRequired<ulong>(nameof(MuteRole));
            MuteReactionFor = section.GetRequired<string>(nameof(MuteReactionFor)).ToDiscordEmote();
            MuteReactionAgainst = section.GetRequired<string>(nameof(MuteReactionAgainst)).ToDiscordEmote();
            MuteVoteMinVotes = section.GetRequired<int>(nameof(MuteVoteMinVotes));
            MuteVoteMaxVotes = section.GetRequired<int>(nameof(MuteVoteMaxVotes));
        }
    }
}