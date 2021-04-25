using Discord;
using Discord.Commands;
using Discord.Rest;
using System.Threading.Tasks;

namespace Inkluzitron.Modules
{
    public class ModuleBase : ModuleBase<SocketCommandContext>
    {
        protected Task<IUserMessage> ReplyAsync(string message = null, bool isTTS = false, Embed embed = null, AllowedMentions allowedMentions = null)
        {
            var options = RequestOptions.Default;
            var reference = new MessageReference(Context.Message.Id, Context.Channel.Id, Context.Guild.Id);
            allowedMentions = CheckAndFixAllowedMentions(allowedMentions);

            return base.ReplyAsync(message, isTTS, embed, options, allowedMentions, reference);
        }

        protected Task<RestUserMessage> ReplyFileAsync(string filePath, AllowedMentions allowedMentions = null)
        {
            var options = RequestOptions.Default;
            var reference = new MessageReference(Context.Message.Id, Context.Channel.Id, Context.Guild.Id);
            allowedMentions = CheckAndFixAllowedMentions(allowedMentions);

            return Context.Channel.SendFileAsync(filePath, options: options, allowedMentions: allowedMentions, messageReference: reference);
        }

        static protected AllowedMentions CheckAndFixAllowedMentions(AllowedMentions allowedMentions)
        {
            // Override default behaviour. Mention only replied user
            return allowedMentions ?? new AllowedMentions() { MentionRepliedUser = true };
        }
    }
}
