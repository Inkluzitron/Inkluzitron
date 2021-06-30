using Inkluzitron.Data.Entities;
using System.Threading.Tasks;

namespace Inkluzitron.Contracts
{
    public interface IScheduledTaskHandler
    {
        Task<bool> TryHandleAsync(ScheduledTask scheduledTask);
    }
}
