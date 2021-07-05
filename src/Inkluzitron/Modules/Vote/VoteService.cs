using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Inkluzitron.Data;
using Inkluzitron.Data.Entities;
using Inkluzitron.Handlers;
using Inkluzitron.Models.Settings;
using Inkluzitron.Models.Vote;
using Inkluzitron.Services;
using Inkluzitron.Utilities;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Inkluzitron.Modules.Vote
{
    public sealed class VoteService : IDisposable
    {
        private DiscordSocketClient Client { get; }
        private ScheduledTasksService ScheduledTasksService { get; }
        private VoteDefinitionParser Parser { get; }
        private DatabaseFactory DbFactory { get; }
        private MessagesHandler MessagesHandler { get; }
        private VoteTranslations VoteTranslations { get; }

        private SemaphoreSlim ReplySemaphore { get; } = new SemaphoreSlim(1);

        public VoteService(DiscordSocketClient client, ScheduledTasksService scheduledTasksService, VoteDefinitionParser parser, DatabaseFactory dbFactory, MessagesHandler messagesHandler, VoteTranslations voteTranslations)
        {
            MessagesHandler = messagesHandler;
            Client = client;
            ScheduledTasksService = scheduledTasksService;
            Parser = parser;
            DbFactory = dbFactory;
            VoteTranslations = voteTranslations;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            ReplySemaphore.Dispose();
        }

        public async Task<(bool success, string commandName, string commandArgs)> TryMatchVoteCommand(IMessage message)
        {
            if (message is not IUserMessage userMessage)
                return (false, null, null);

            if (userMessage.Author.IsBot)
                return (false, null, null);

            var (success, commandName, commandArgs) = await MessagesHandler.TryMatchSingleCommand(userMessage);
            if (!success || !VoteModule.VoteStartingCommands.Contains(commandName))
                return (false, null, null);

            return (success, commandName, commandArgs);
        }

        private static bool ExtractGuildId(IChannel channel, out ulong guildId)
        {
            if (channel is IGuildChannel guildChannel)
            {
                guildId = guildChannel.GuildId;
                return true;
            }

            guildId = default;
            return false;
        }

        public async Task<bool> DeleteAssociatedVoteReplyIfExistsAsync(IChannel channel, ulong messageId)
        {
            if (channel is not ITextChannel textChannel)
                return false;
            if (!ExtractGuildId(channel, out var guildId))
                return false;

            using var dbContext = DbFactory.Create();

            var voteReplyRecord = await dbContext.VoteReplyRecords.AsQueryable().SingleOrDefaultAsync(
                r => r.GuildId == guildId && r.ChannelId == channel.Id && r.MessageId == messageId
            );

            if (voteReplyRecord is null)
                return false;

            var voteReply = await LocateVoteReply(dbContext, textChannel, voteReplyRecord);
            if (voteReply is null)
                return false;

            await voteReply.DeleteAsync();

            dbContext.VoteReplyRecords.Remove(voteReplyRecord);
            await dbContext.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteVoteReplyRecordIfExistsAsync(IChannel channel, ulong messageId)
        {
            if (!ExtractGuildId(channel, out var guildId))
                return false;

            using var dbContext = DbFactory.Create();
            var voteReplyRecord = await dbContext.VoteReplyRecords.AsQueryable()
                .Where(r => r.GuildId == guildId && r.ChannelId == channel.Id && r.ReplyId == messageId)
                .FirstOrDefaultAsync();

            if (voteReplyRecord is null)
                return false;

            dbContext.VoteReplyRecords.Remove(voteReplyRecord);
            await dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<VoteDefinition> ParseVoteCommand(IUserMessage userMessage)
        {
            var (success, commandName, commandArgs) = await TryMatchVoteCommand(userMessage);
            if (!success)
                return null;

            if (!VoteModule.VoteStartingCommands.Contains(commandName))
                return null;

            var commandContext = new CommandContext(Client, userMessage);
            var parseResult = await Parser.TryParse(commandContext, commandArgs);
            return parseResult.Success ? parseResult.Definition : null;
        }

        private async Task<IUserMessage> LocateVoteReplyTo(IChannel channel, ulong messageId)
        {
            if (channel is not ITextChannel textChannel)
                return null;

            if (!ExtractGuildId(channel, out var guildId))
                return null;

            using var dbContext = DbFactory.Create();

            var voteReplyRecord = await dbContext.VoteReplyRecords.AsQueryable().SingleOrDefaultAsync(
                r => r.GuildId == guildId && r.ChannelId == channel.Id && r.MessageId == messageId
            );

            if (voteReplyRecord is null)
                return null;

            return await LocateVoteReply(dbContext, textChannel, voteReplyRecord);
        }

        static private async Task<IUserMessage> LocateVoteReply(BotDatabaseContext dbContext, ITextChannel channel, VoteReplyRecord voteReplyRecord)
        {
            var voteReply = await channel.GetMessageAsync(voteReplyRecord.ReplyId) as IUserMessage;
            if (voteReply is null)
            {
                dbContext.VoteReplyRecords.Remove(voteReplyRecord);
                await dbContext.SaveChangesAsync();
            }

            return voteReply;
        }

        public async Task UpdateVoteReplyAsync(IMessage voteCommandMessage, string voteReplyText)
        {
            var existingVoteReply = await LocateVoteReplyTo(voteCommandMessage.Channel, voteCommandMessage.Id);
            if (existingVoteReply is not null)
            {
                await existingVoteReply.ModifyAsync(p => p.Content = voteReplyText);
                return;
            }

            if (!ExtractGuildId(voteCommandMessage.Channel, out var guildId))
                return;

            await ReplySemaphore.WaitAsync();

            try
            {
                existingVoteReply = await LocateVoteReplyTo(voteCommandMessage.Channel, voteCommandMessage.Id);
                if (existingVoteReply is not null)
                {
                    await existingVoteReply.ModifyAsync(p => p.Content = voteReplyText);
                    return;
                }

                var voteCommandReference = new MessageReference(
                    voteCommandMessage.Id,
                    voteCommandMessage.Channel.Id,
                    guildId
                );

                var newVoteReply = await voteCommandMessage.Channel.SendMessageAsync(
                    text: voteReplyText,
                    allowedMentions: AllowedMentions.None,
                    messageReference: voteCommandReference
                );

                using var dbContext = DbFactory.Create();
                dbContext.VoteReplyRecords.Add(new VoteReplyRecord
                {
                    GuildId = guildId,
                    ChannelId = voteCommandReference.ChannelId,
                    MessageId = voteCommandMessage.Id,
                    ReplyId = newVoteReply.Id
                });

                await dbContext.SaveChangesAsync();
            }
            finally
            {
                ReplySemaphore.Release();
            }
        }

        public async Task ProcessVoteCommandAsync(IUserMessage voteCommandMessage, string voteDefinitionText)
        {
            if (voteCommandMessage.Channel is not IGuildChannel guildChannel)
                return;

            var parse = await Parser.TryParse(new CommandContext(Client, voteCommandMessage), voteDefinitionText);

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

            var tail = failedEmotes.Count == 0
                ? string.Empty
                : Environment.NewLine + string.Format(
                    VoteTranslations.UnaccessibleEmotes,
                    new FormatByValue(failedEmotes.Count),
                    string.Join(", ", failedEmotes.Select(e => e.ToString()))
                );

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

        public string ComposeSummary(IUserMessage message, VoteDefinition def)
        {
            var currentResults = message.Reactions.ToDictionary(r => r.Key, r => r.Value.ReactionCount - (r.Value.IsMe ? 1 : 0));
            var winningVoteCount = currentResults.Count > 0 ? currentResults.Max(kvp => kvp.Value) : 0;
            var winners = winningVoteCount == 0
                ? Array.Empty<IEmote>()
                : currentResults
                    .Where(kvp => kvp.Value == winningVoteCount)
                    .Select(kvp => kvp.Key)
                    .Where(def.Options.ContainsKey)
                    .ToArray();

            var optionsList = string.Join(", ", winners.Select(winner => "**" + Format.Sanitize(def.Options[winner]) + "**"));
            var voteIsUnderway = def.Deadline is not DateTimeOffset deadline || DateTimeOffset.UtcNow < deadline;
            var translations = voteIsUnderway ? VoteTranslations.VoteUnderway : VoteTranslations.VoteFinished;
            var lines = new List<string>();

            if (winners.Length == 0)
            {
                lines.Add(translations.NoWinners);
            }
            else
            {
                lines.Add(string.Format(
                    winners.Length == 1 ? translations.OneWinner : translations.MultipleWinners,

                    optionsList,
                    winningVoteCount,
                    new FormatByValue(winningVoteCount)
                ));
            }

            if (def.Deadline is DateTimeOffset voteDeadline)
                lines.Add(string.Format(translations.DeadlineNotice, voteDeadline.ToLocalTime()));

            return string.Join(Environment.NewLine, lines);
        }
    }
}
