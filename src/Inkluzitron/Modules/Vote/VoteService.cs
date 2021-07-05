using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Inkluzitron.Data;
using Inkluzitron.Data.Entities;
using Inkluzitron.Handlers;
using Inkluzitron.Models.Settings;
using Inkluzitron.Models.Vote;
using Inkluzitron.Services;
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

        public bool TryMatchVoteCommand(IMessage message, out string commandName, out string commandArgs)
        {
            commandName = commandArgs = null;

            if (message is not IUserMessage userMessage)
                return false;

            if (userMessage.Author.IsBot)
                return false;

            if (!MessagesHandler.TryMatchSingleCommand(userMessage, out commandName, out commandArgs))
                return false;

            return VoteModule.VoteStartingCommands.Contains(commandName);
        }

        private static bool ExtractGuildId(IChannel channel, out ulong guildId)
        {
            if (channel is IGuildChannel guildChannel)
            {
                guildId = guildChannel.Id;
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

            var voteReplyRecord = await dbContext.VoteReplyRecords.FindAsync(guildId, channel.Id, messageId);
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
            if (!MessagesHandler.TryMatchSingleCommand(userMessage, out var commandName, out var commandArgs))
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

            var voteReplyRecord = await dbContext.VoteReplyRecords.FindAsync(guildId, channel.Id, messageId);
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
                    (voteCommandMessage.Channel as IGuildChannel)?.GuildId
                );

                var newVoteReply = await voteCommandMessage.Channel.SendMessageAsync(
                    text: voteReplyText,
                    allowedMentions: AllowedMentions.None,
                    messageReference: voteCommandReference
                );

                using var dbContext = DbFactory.Create();

                dbContext.VoteReplyRecords.Add(new VoteReplyRecord
                {
                    GuildId = voteCommandReference.GuildId.Value,
                    ChannelId = voteCommandReference.ChannelId,
                    MessageId = voteCommandReference.MessageId.Value,
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

            var optionsList = string.Join(", ", winners.Select(w => "**" + Format.Sanitize(def.Options[w]) + "**"));
            var voteIsUnderway = def.Deadline is DateTimeOffset deadline && DateTimeOffset.UtcNow < deadline;
            var translations = voteIsUnderway ? VoteTranslations.VoteUnderway : VoteTranslations.VoteFinished;
            var lines = new List<string>();

            if (winners.Length == 0)
                lines.Add(translations.NoWinners);
            else
            {
                lines.Add(string.Format(
                    winners.Length == 1 ? translations.OneWinner : translations.MultipleWinners,

                    optionsList,
                    winningVoteCount,
                    winningVoteCount // TODO formatter                    
                ));
            }

            return string.Join(Environment.NewLine, lines);
        }
    }
}
