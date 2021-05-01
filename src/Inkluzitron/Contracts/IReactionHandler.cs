using Discord;
using System.Threading.Tasks;

namespace Inkluzitron.Contracts
{
    public interface IReactionHandler
    {
        Task<bool> Handle(IUserMessage message, IEmote reaction, IUser user);
    }
}
