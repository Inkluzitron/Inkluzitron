using Discord;
using Discord.Commands;
using Discord.Rest;
using System.Threading.Tasks;

namespace Inkluzitron.Modules
{
    public class ModuleBase : ModuleBase<SocketCommandContext>
    {
        private MessageReference ReplyReference => new MessageReference(Context.Message.Id, Context.Channel.Id, Context.Guild?.Id);

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
            // Override default behaviour. Mention only replied user
            return allowedMentions ?? new AllowedMentions() { MentionRepliedUser = true };
        }
    }
}
