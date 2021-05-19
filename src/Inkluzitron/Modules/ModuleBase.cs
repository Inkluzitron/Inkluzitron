using Discord;
using Discord.Commands;
using Discord.Rest;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Inkluzitron.Modules
{
    public class ModuleBase : ModuleBase<SocketCommandContext>
    {
        private MessageReference ReplyReference => new(Context.Message.Id, Context.Channel.Id, Context.Guild?.Id);

        protected async Task<List<IUserMessage>> ReplyAsync(IEnumerable<string> messages, bool isTTS = false, AllowedMentions allowedMentions = null)
        {
            if (!messages.Any()) // No messages
                return new List<IUserMessage>();

            allowedMentions = CheckAndFixAllowedMentions(allowedMentions);

            // Apply message reference (reply) only for first message.
            var resultMessages = new List<IUserMessage>
            {
                await ReplyAsync(messages.First(), isTTS, null, allowedMentions)
            };

            foreach (var msg in messages.Skip(1))
                resultMessages.Add(await ReplyAsync(msg, false, null, null, allowedMentions, null));

            return resultMessages;
        }

        protected Task<IUserMessage> ReplyAsync(string message = null, bool isTTS = false, Embed embed = null, AllowedMentions allowedMentions = null)
        {
            var options = RequestOptions.Default;
            allowedMentions = CheckAndFixAllowedMentions(allowedMentions);

            return base.ReplyAsync(message, isTTS, embed, options, allowedMentions, ReplyReference);
        }

        protected Task<RestUserMessage> ReplyFileAsync(string filePath, string text = null, AllowedMentions allowedMentions = null)
        {
            var options = RequestOptions.Default;
            allowedMentions = CheckAndFixAllowedMentions(allowedMentions);

            return Context.Channel.SendFileAsync(filePath, text: text, options: options, allowedMentions: allowedMentions, messageReference: ReplyReference);
        }

        static protected AllowedMentions CheckAndFixAllowedMentions(AllowedMentions allowedMentions)
        {
            // Override default behaviour. Do not mention replied user
            return allowedMentions ?? new AllowedMentions() { MentionRepliedUser = false };
        }
    }
}
