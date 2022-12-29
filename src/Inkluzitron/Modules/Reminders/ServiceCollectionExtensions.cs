using Inkluzitron.Contracts;
using Inkluzitron.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Inkluzitron.Modules.Reminders
{
    static public class ServiceCollectionExtensions
    {
        static public IServiceCollection AddRemindersModule(this IServiceCollection services)
            => services
                .AddSingletonWithInterface<ReminderScheduledTaskHandler, IScheduledTaskHandler>()
                .AddTransient<ReminderManager>();
    }
}
