using Discord;
using Discord.Commands;
using System.Threading.Tasks;

namespace Inkluzitron.Modules
{
    public class ModuleBase : ModuleBase<SocketCommandContext>
    {
        protected Task<IUserMessage> ReplyAsync(string message = null, bool isTTS = false, Embed embed = null, AllowedMentions allowedMentions = null)
        {
            var options = RequestOptions.Default;
            var reference = new MessageReference(Context.Message.Id, Context.Channel.Id, Context.Guild.Id);

            return base.ReplyAsync(message, isTTS, embed, options, allowedMentions, reference);
        }
    }
}
