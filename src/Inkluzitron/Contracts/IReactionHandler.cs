using Discord;
using System.Threading.Tasks;

namespace Inkluzitron.Contracts
{
    public interface IReactionHandler
    {
        Task<bool> HandleAsync(IUserMessage message, IEmote reaction, IUser user, IUser botUser);
    }
}
