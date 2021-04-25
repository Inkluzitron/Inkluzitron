using Discord;
using Discord.Commands;
using Discord.Rest;
using System.IO;
using System.Threading.Tasks;

namespace Inkluzitron.Modules
{
    public class ModuleBase : ModuleBase<SocketCommandContext>
    {
        protected MessageReference ReplyReference => new MessageReference(Context.Message.Id, Context.Channel.Id, Context.Guild?.Id);

        protected Task<IUserMessage> ReplyAsync(string message = null, bool isTTS = false, Embed embed = null, AllowedMentions allowedMentions = null)
        {
            allowedMentions = CheckAndFixAllowedMentions(allowedMentions);
            return base.ReplyAsync(message, isTTS, embed, null, allowedMentions, ReplyReference);
        }

        protected Task<RestUserMessage> ReplyFileAsync(string filePath, AllowedMentions allowedMentions = null)
        {
            allowedMentions = CheckAndFixAllowedMentions(allowedMentions);

            return Context.Channel.SendFileAsync(filePath, options: null, allowedMentions: allowedMentions, messageReference: ReplyReference);
        }

        protected Task<RestUserMessage> ReplyStreamAsync(Stream stream, string filename, bool spoiler = false, string text = null, bool isTTS = false, Embed embed = null, AllowedMentions allowedMentions = null)
        {
            allowedMentions = CheckAndFixAllowedMentions(allowedMentions);
            return Context.Channel.SendFileAsync(stream, filename, text, isTTS, embed, null, spoiler, allowedMentions, ReplyReference);
        }

        static protected AllowedMentions CheckAndFixAllowedMentions(AllowedMentions allowedMentions)
        {
            // Override default behaviour. Mention only replied user
            return allowedMentions ?? new AllowedMentions() { MentionRepliedUser = true };
        }
    }
}
