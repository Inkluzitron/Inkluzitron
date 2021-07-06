using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Inkluzitron.Extensions
{
    static public class ServiceCollectionExtensions
    {
        static public IServiceCollection AddSingletonWithInterface<TSingleton, TService>(this IServiceCollection serviceCollection)
            where TSingleton : class, TService
            where TService : class
        {
            return serviceCollection
                .AddSingleton<TSingleton>()
                .AddSingleton<TService>(sp => sp.GetRequiredService<TSingleton>());
        }

        static public IServiceCollection RegisterAs<TSingleton, TService>(this IServiceCollection serviceCollection)
            where TSingleton : class, TService
            where TService : class
        {
            return serviceCollection.RegisterAs(typeof(TSingleton), typeof(TService));
        }

        static public IServiceCollection RegisterAs(this IServiceCollection serviceCollection, Type singletonType, Type serviceType)
        {
            serviceCollection.TryAddSingleton(singletonType);
            return serviceCollection.AddSingleton(serviceType, sp => sp.GetRequiredService(singletonType));
        }
    }
}
