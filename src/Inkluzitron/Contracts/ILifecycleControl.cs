using System.Threading.Tasks;

namespace Inkluzitron.Contracts
{
    public interface ILifecycleControl
    {
        Task StartAsync();
        Task StopAsync();
    }
}
