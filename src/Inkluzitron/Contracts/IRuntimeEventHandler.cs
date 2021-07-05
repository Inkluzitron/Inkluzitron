using System.Threading.Tasks;

namespace Inkluzitron.Contracts
{
    public interface IRuntimeEventHandler
    {
        Task OnBotStartingAsync() => Task.CompletedTask;
        Task OnHomeGuildReadyAsync() => Task.CompletedTask;
        Task OnBotStoppingAsync() => Task.CompletedTask;
    }
}
