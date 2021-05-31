using Discord;
using Inkluzitron.Extensions;
using Microsoft.Extensions.Configuration;
using System;

namespace Inkluzitron.Models.Settings
{
    public class VoteSettings
    {
        static private readonly string SettingsSectionName = "VoteModule";

        public ulong MuteRoleId { get; set; }
        public IEmote MuteReactionFor { get; set; }
        public IEmote MuteReactionAgainst { get; set; }
        public int MuteVoteMinVotes { get; set; }
        public int MuteVoteMaxVotes { get; set; }
        public int MuteMinutesMin { get; set; }
        public int MuteMinutesMax { get; set; }
        public TimeSpan MuteVoteTimeout { get; set; }
        public int MuteVoteTimeoutMinutes { get; set; }

        public VoteSettings(IConfiguration config)
        {
            var section = config.GetSection(SettingsSectionName);
            MuteRoleId = section.GetRequired<ulong>(nameof(MuteRoleId));
            MuteReactionFor = section.GetRequired<string>(nameof(MuteReactionFor)).ToDiscordEmote();
            MuteReactionAgainst = section.GetRequired<string>(nameof(MuteReactionAgainst)).ToDiscordEmote();
            MuteVoteMinVotes = section.GetRequired<int>(nameof(MuteVoteMinVotes));
            MuteVoteMaxVotes = section.GetRequired<int>(nameof(MuteVoteMaxVotes));
            MuteMinutesMin = section.GetRequired<int>(nameof(MuteMinutesMin));
            MuteMinutesMax = section.GetRequired<int>(nameof(MuteMinutesMax));
            MuteVoteTimeoutMinutes = section.GetRequired<int>(nameof(MuteVoteTimeoutMinutes));
            MuteVoteTimeout = new TimeSpan(0, MuteVoteTimeoutMinutes, 0);
        }
    }
}
