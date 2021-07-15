using Inkluzitron.Data.Entities;
using Inkluzitron.Enums;
using System.Threading.Tasks;

namespace Inkluzitron.Contracts
{
    public interface IScheduledTaskHandler
    {
        Task<ScheduledTaskResult> HandleAsync(ScheduledTask scheduledTask);
    }
}
