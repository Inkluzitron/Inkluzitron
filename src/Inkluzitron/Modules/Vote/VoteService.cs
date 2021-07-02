using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Inkluzitron.Data.Entities;
using Inkluzitron.Models;
using Inkluzitron.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Inkluzitron.Modules.Vote
{
    public class VoteService
    {
        private string CommandPrefix { get; }
        private DiscordSocketClient Client { get; }
        private ScheduledTasksService ScheduledTasksService { get; }
        private VoteDefinitionParser Parser { get; }
        private IMemoryCache Cache { get; }

        public VoteService(DiscordSocketClient client, ScheduledTasksService scheduledTasksService, VoteDefinitionParser parser, IMemoryCache cache, IConfiguration config)
        {
            CommandPrefix = config["Prefix"];
            Client = client;
            ScheduledTasksService = scheduledTasksService;
            Parser = parser;
            Cache = cache;

            // TODO: interfaces
            Client.MessageDeleted += Client_MessageDeleted;
            Client.MessageUpdated += Client_MessageUpdated;
        }

        private bool IsVoteCommand(IMessage userMessage)
            => userMessage != null
            && !userMessage.Author.IsBot
            && userMessage.Content.StartsWith("$vote"); // todo: unify with parser

        private bool WasVoteCommand(Cacheable<IMessage, ulong> changedOrDeletedMessage, IMessageChannel channel)
        {
            var msg = changedOrDeletedMessage;
            if (channel is not IGuildChannel guildChannel)
                return false;

            var cacheKey = GetReplyMessageCacheKey(guildChannel.Guild.Id, guildChannel.Id, msg.Id);
            if (Cache.TryGetValue(cacheKey, out _))
                return true;

            if (msg.HasValue && msg.Value is IUserMessage deletedUserMessage)
                return IsVoteCommand(deletedUserMessage);

            return false;
        }

        private async Task Client_MessageUpdated(Cacheable<IMessage, ulong> oldMessage, SocketMessage newMessage, ISocketMessageChannel sourceChannel)
        {
            if (IsVoteCommand(newMessage) || WasVoteCommand(oldMessage, sourceChannel))
            {
                // newMessage has Reactions.Count == 0, always
                var freshMessage = await sourceChannel.GetMessageAsync(newMessage.Id);
                if (freshMessage is IUserMessage freshUserMessage)
                    await ProcessVoteCommandAsync(freshUserMessage);
            }
        }

        private async Task Client_MessageDeleted(Cacheable<IMessage, ulong> deletedMessage, ISocketMessageChannel sourceChannel)
        {
            if (sourceChannel is not IGuildChannel guildChannel)
                return;

            if (!WasVoteCommand(deletedMessage, sourceChannel))
                return;

            var reply = deletedMessage.HasValue
                ? await LocateVoteReplyTo(deletedMessage.Value)
                : await LocateVoteReplyTo(GetReplyMessageCacheKey(
                    guildChannel.GuildId,
                    guildChannel.Id,
                    deletedMessage.Id
                ), sourceChannel);

            if (reply != null)
                await reply.DeleteAsync();
        }

        private static string GetReplyMessageCacheKey(IMessage voteMessage)
            => GetReplyMessageCacheKey(
                (voteMessage.Channel as IGuildChannel)?.GuildId ?? 0,
                voteMessage.Channel.Id,
                voteMessage.Id
            );

        private static string GetReplyMessageCacheKey(ulong guildId, ulong channelId, ulong messageId)
            => $"VoteReply/{guildId}/{channelId}/{messageId}";

        private bool IsVoteReply(IMessage voteReplyCandidate, IMessage voteMessage)
            => voteReplyCandidate.Author.IsBot
            && voteReplyCandidate.Author.Id == Client.CurrentUser.Id
            && voteReplyCandidate.Reference is MessageReference msgReference
            && voteMessage.Channel is IGuildChannel guildChannel
            && msgReference.GuildId.GetValueOrDefault() == guildChannel.Guild.Id
            && msgReference.ChannelId == voteMessage.Channel.Id
            && msgReference.MessageId.GetValueOrDefault() == voteMessage.Id;

        public async Task<VoteDefinition> TryParseVoteCommand(IUserMessage userMessage)
        {
            var commandContext = new CommandContext(Client, userMessage);
            var parseResult = await Parser.TryParse(commandContext, CommandPrefix, userMessage.Content);
            return parseResult.Success ? parseResult.Definition : null;
        }

        private async Task<IUserMessage> LocateVoteReplyTo(string cacheKey, IMessageChannel channel)
        {
            if (Cache.TryGetValue<ulong>(cacheKey, out var replyMessageId) && await channel.GetMessageAsync(replyMessageId) is IUserMessage cachedVoteReply)
            {
                return cachedVoteReply;
            }

            return null;
        }

        private async Task<IUserMessage> LocateVoteReplyTo(IMessage voteCommandMessage)
        {
            var cacheKey = GetReplyMessageCacheKey(voteCommandMessage);
            var reply = await LocateVoteReplyTo(cacheKey, voteCommandMessage.Channel);

            if (reply is not null)
                return reply;

            await foreach (var page in voteCommandMessage.Channel.GetMessagesAsync(voteCommandMessage, Direction.After))
            {
                foreach (var replyCandidate in page)
                {
                    if (IsVoteReply(replyCandidate, voteCommandMessage) && replyCandidate is IUserMessage foundVoteReply)
                    {
                        Cache.Set(cacheKey, foundVoteReply.Id);
                        return foundVoteReply;
                    }
                }
            }

            return null;
        }

        public async Task UpdateVoteReplyAsync(IMessage voteCommandMessage, string voteReplyText)
        {
            if (await LocateVoteReplyTo(voteCommandMessage) is IUserMessage existingVoteReply)
            {
                await existingVoteReply.ModifyAsync(p => p.Content = voteReplyText);
                return;
            }

            var newVoteReply = await voteCommandMessage.Channel.SendMessageAsync(
                text: voteReplyText,
                allowedMentions: AllowedMentions.None,
                messageReference: new MessageReference(
                    voteCommandMessage.Id,
                    voteCommandMessage.Channel.Id,
                    (voteCommandMessage.Channel as IGuildChannel)?.GuildId
                )
            );

            var cacheKey = GetReplyMessageCacheKey(voteCommandMessage);
            Cache.Set(cacheKey, newVoteReply.Id);
        }

        public async Task ProcessVoteCommandAsync(IUserMessage voteCommandMessage)
        {
            if (voteCommandMessage.Channel is not IGuildChannel guildChannel)
                return;

            var parse = await Parser.TryParse(new CommandContext(Client, voteCommandMessage), CommandPrefix, voteCommandMessage.Content);

            if (!parse.Success)
            {
                await UpdateVoteReplyAsync(voteCommandMessage, parse.ProblemDescription);
                return;
            }

            var allEmotes = new HashSet<IEmote>(parse.Definition.Options.Keys);
            allEmotes.UnionWith(voteCommandMessage.Reactions.Keys);
            var failedEmotes = new List<IEmote>();
            var summary = ComposeSummary(voteCommandMessage, parse.Definition);

            foreach (var emote in allEmotes)
            {
                var shouldBePresent = parse.Definition.Options.ContainsKey(emote);
                var isPresent = voteCommandMessage.Reactions.TryGetValue(emote, out var reaction);

                if (!shouldBePresent && isPresent)
                {
                    await voteCommandMessage.RemoveAllReactionsForEmoteAsync(emote);
                }
                else if (shouldBePresent && !isPresent)
                {
                    try
                    {
                        await voteCommandMessage.AddReactionAsync(emote);
                    }
                    catch
                    {
                        failedEmotes.Add(emote);
                    }
                }
            }

            var tailItems = new List<string>();

            if (parse.Notice is string parseNotice)
                tailItems.Add(parse.Notice);

            if (failedEmotes.Count > 0)
                tailItems.Add("Následující reakce jsem nemohl nastřelit, protože k nim nemám přístup: " + string.Join(", ", failedEmotes.Select(e => e.ToString())));

            var tail = string.Concat(tailItems.Select(item => Environment.NewLine + item));
            await UpdateVoteReplyAsync(voteCommandMessage, summary + tail);

            if (parse.Definition.Deadline is DateTimeOffset votingDeadline)
            {
                var scheduledTaskTag = voteCommandMessage.GetJumpUrl();

                foreach (var existingScheduledTask in await ScheduledTasksService.LookupAsync("EndOfVoting", scheduledTaskTag))
                    await ScheduledTasksService.CancelAsync(existingScheduledTask.ScheduledTaskId);

                await ScheduledTasksService.EnqueueAsync(new ScheduledTask
                {
                    Discriminator = "EndOfVoting",
                    Tag = scheduledTaskTag,
                    When = votingDeadline,
                    Data = JsonConvert.SerializeObject(new EndOfVotingScheduledTaskData
                    {
                        GuildId = guildChannel.GuildId,
                        ChannelId = guildChannel.Id,
                        MessageId = voteCommandMessage.Id
                    })
                });
            }
        }

        static public string ComposeSummary(IUserMessage message, VoteDefinition def)
        {
            var counts = message.Reactions.ToDictionary(r => r.Key, r => r.Value.ReactionCount - (r.Value.IsMe ? 1 : 0));
            var winningVoteCount = counts.Any() ? counts.Max(kvp => kvp.Value) : 0;
            var winners = winningVoteCount == 0
                ? Array.Empty<IEmote>()
                : counts
                    .Where(kvp => kvp.Value == winningVoteCount)
                    .Select(kvp => kvp.Key)
                    .Where(def.Options.ContainsKey)
                    .ToArray();

            var optionsList = string.Join(", ", winners.Select(w => "**" + Format.Sanitize(def.Options[w]) + "**"));

            if (def.Deadline is DateTimeOffset deadline && DateTimeOffset.UtcNow > deadline)
            {
                if (winners.Length == 0)
                    return "Hlasování skončilo. Nevyhrála žádná možnost.";
                else if (winners.Length == 1)
                    return $"Hlasování skončilo. Vyhrála možnost {optionsList} s {winningVoteCount} hlas{(winningVoteCount == 1 ? "em" : "y")}.";
                else
                    return $"Hlasování skončilo. Vyhrály možnosti {optionsList} s {winningVoteCount} hlas{(winningVoteCount == 1 ? "em" : "y")}.";
            }

            var tail = string.Empty;
            if (def.Deadline is DateTimeOffset něgdy)
                tail = $"Hlasování skončí {něgdy.ToLocalTime().ToString("G", CultureInfo.GetCultureInfo("cs-CZ"))}";

            if (winners.Length == 0)
                return $"Nikdo zatím nehlasoval. {tail}";
            else if (winners.Length == 1)
                return $"Vyhrává možnost {optionsList} s {winningVoteCount} hlas{(winningVoteCount == 1 ? "em" : "y")}. {tail}";
            else
                return $"Vyhrávájí možnosti {optionsList} s {winningVoteCount} hlas{(winningVoteCount == 1 ? "em" : "y")}. {tail}";
        }
    }
}
