using System.Threading.Tasks;

namespace Inkluzitron.Contracts
{
    public interface ILifecycleControl
    {
        StartCondition WhenToStart { get; }
        bool IsStarted { get; }
        Task StartAsync();
        Task StopAsync();
    }
}
