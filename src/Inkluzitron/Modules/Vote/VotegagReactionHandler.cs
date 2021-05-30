using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Inkluzitron.Contracts;
using Inkluzitron.Enums;
using Inkluzitron.Extensions;
using Inkluzitron.Models.Settings;
using Inkluzitron.Modules.Vote;
using Inkluzitron.Services;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Inkluzitron.Modules.Help
{
    public class VotegagReactionHandler : IReactionHandler
    {
        private UsersService UsersService { get; }
        private VoteSettings VoteSettings { get; }
        private BotSettings BotSettings { get; }
        private DiscordSocketClient Client { get; }
        private IMemoryCache Cache { get; }

        static private readonly Regex VoteMessageMatch
            = new(@"^Hlasování o umlčení.*?(\S+) na dobu \*\*(\d+).*?alespoň (\d+).*?Hlasování bude", RegexOptions.Singleline);

        public VotegagReactionHandler(UsersService usersService, VoteSettings voteSettings,
            DiscordSocketClient client, IMemoryCache cache, BotSettings botSettings)
        {
            UsersService = usersService;
            VoteSettings = voteSettings;
            Client = client;
            Cache = cache;
            BotSettings = botSettings;

            client.Ready += ClientReadyAsync;
            client.ChannelCreated += UpdateChannelMutedRole;
        }

        private async Task UpdateChannelMutedRole(SocketChannel channel)
        {
            if (channel is not SocketGuildChannel guildChannel) return;

            var muteRole = guildChannel.Guild.Roles.First(r => r.Id == VoteSettings.MuteRole);

            var isPermSet = guildChannel.PermissionOverwrites.Any(o =>
                 o.TargetType == PermissionTarget.Role &&
                 o.TargetId == muteRole.Id &&
                 o.Permissions.SendMessages == PermValue.Deny);

            if (isPermSet) return;

            var rolePerms = new OverwritePermissions(sendMessages: PermValue.Deny);

            await guildChannel.AddPermissionOverwriteAsync(muteRole, rolePerms);
            
        }

        private async Task ClientReadyAsync()
        {
            var guild = Client.GetGuild(BotSettings.HomeGuildId);
            var muteRole = guild.Roles.First(r => r.Id == VoteSettings.MuteRole);

            await SetupMutedRole(guild);

            foreach (var user in guild.Users)
            {
                var userDb = await UsersService.GetUserDbEntityAsync(user);

                if (userDb == null || userDb.MutedUntil == null)
                {
                    if(user.Roles.Any(r => r.Id == muteRole.Id))
                        await user.RemoveRoleAsync(muteRole);
                    continue;
                }

                if (userDb.MutedUntil <= DateTime.Now)
                {
                    await user.RemoveRoleAsync(muteRole);
                    await UsersService.SetMutedUntilAsync(user, null);
                    continue;
                }

                ScheduleUnmute(user, (DateTime)userDb.MutedUntil, muteRole);
            }
        }

        private async Task SetupMutedRole(SocketGuild guild)
        {
            foreach (var channel in guild.Channels)
            {
                await UpdateChannelMutedRole(channel);
            }
        }

        private void ScheduleUnmute(SocketGuildUser user, DateTime endTime, SocketRole mutedRole)
        {
            Task.Delay(endTime - DateTime.Now).ContinueWith(_ =>
                VotegagModule.UnmuteUser(user, UsersService, mutedRole));
        }

        public async Task<bool> HandleReactionChangedAsync(IUserMessage message, IEmote reaction, IUser user, ReactionEvent eventType)
        {
            if (message.Author.Id != Client.CurrentUser.Id)
                return false; // Author check

            if (message.ReferencedMessage == null)
                return false;

            if (message.MentionedUserIds.Count == 0)
                return false;

            var match = VoteMessageMatch.Match(message.Content);
            if (!match.Success)
                return false;

            if(!reaction.IsEqual(VoteSettings.MuteReactionFor) && !reaction.IsEqual(VoteSettings.MuteReactionAgainst))
            {
                await message.RemoveReactionAsync(reaction, user);
                return true;
            }

            var targetId = message.MentionedUserIds.First();

            if (!Cache.TryGetValue<DateTime>(VotegagModule.CreateCacheKey(targetId), out var voteEnd))
                return false; // Check if vote exists

            if (voteEnd <= DateTime.Now)
                return false; // Check for vote end

            var targetMention = match.Groups[1].Value;
            var muteTime = int.Parse(match.Groups[2].Value);
            var minVotes = int.Parse(match.Groups[3].Value);
            var totalVotes = 0;

            var reactions = message.Reactions;
            if (reactions.TryGetValue(VoteSettings.MuteReactionFor, out var votesFor))
                totalVotes = votesFor.ReactionCount;

            if (reactions.TryGetValue(VoteSettings.MuteReactionAgainst, out var votesAgainst))
                totalVotes -= votesAgainst.ReactionCount;

            await message.ModifyAsync(msg =>
                msg.Content = VotegagModule.GenerateVoteMessage(targetMention, muteTime, minVotes, totalVotes, voteEnd));

            return true;
        }
    }
}
