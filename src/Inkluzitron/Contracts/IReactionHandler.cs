using Discord;
using Inkluzitron.Enums;
using System.Threading.Tasks;

namespace Inkluzitron.Contracts
{
    public interface IReactionHandler
    {
        Task<bool> HandleReactionChangedAsync(IUserMessage message, IEmote reaction, IUser user, ReactionEvent eventType)
            => Task.FromResult(false);

        Task<bool> HandleReactionAddedAsync(IUserMessage message, IEmote reaction, IUser user)
            => Task.FromResult(false);

        Task<bool> HandleReactionRemovedAsync(IUserMessage message, IEmote reaction, IUser user)
            => Task.FromResult(false);
    }
}
