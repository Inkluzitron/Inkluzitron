using Discord;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.WebSocket;
using Inkluzitron.Contracts;
using Inkluzitron.Extensions;
using Inkluzitron.Models.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileSystemGlobbing.Internal.Patterns;

namespace Inkluzitron.Modules
{
    public class TinderVoteModule : ModuleBase, IReactionHandler
    {
        private DiscordSocketClient DiscordClient { get; }
        private ulong TinderRoomId { get; }
        private ReactionSettings ReactionSettings { get; }

        public TinderVoteModule(DiscordSocketClient discordClient, IConfiguration configuration, ReactionSettings reactionSettings)
        {
            DiscordClient = discordClient;
            DiscordClient.MessageReceived += OnMessageReceivedAsync;
            TinderRoomId = configuration.GetRequired<ulong>("TinderRoomId");
            ReactionSettings = reactionSettings;
        }

        private bool IsImage(Attachment attachment)
        {
            var extension = Path.GetExtension(attachment.Filename);
            return extension == ".png" || extension == ".jpg" || extension == ".jpeg";
        }

        private async Task OnMessageReceivedAsync(SocketMessage message)
        {
            if (message.Channel.Id == TinderRoomId && message.Attachments.Any())
            {
                if (message is IUserMessage userMessage)
                {
                    await userMessage.AddReactionsAsync(ReactionSettings.TinderReactions);
                }
            }
        }

        public async Task<bool> HandleReactionAddedAsync(IUserMessage message, IEmote reaction, IUser user )
        {
            if (!ReactionSettings.TinderReactions.Any(emote => emote.IsEqual(reaction)))
                return false;

            if (reaction.IsEqual(ReactionSettings.Blobshhh) && message.Reactions[ReactionSettings.Blobshhh].ReactionCount >= 1)
            {
                await message.RemoveAllReactionsAsync();
                return true;
            }

            return true;
        }
    }
}
