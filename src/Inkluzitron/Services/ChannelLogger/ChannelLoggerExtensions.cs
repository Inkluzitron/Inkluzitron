using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using System;

namespace Inkluzitron.Services.ChannelLogger
{
    static public class ChannelLoggerExtensions
    {
        static public ILoggingBuilder AddChannelLogger(
        this ILoggingBuilder builder)
        {
            builder.AddConfiguration();

            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Singleton<ILoggerProvider, ChannelLoggerProvider>());

            LoggerProviderOptions.RegisterProviderOptions
                <ChannelLoggerConfiguration, ChannelLoggerProvider>(builder.Services);

            return builder;
        }

        static public ILoggingBuilder AddChannelLogger(
            this ILoggingBuilder builder,
            Action<ChannelLoggerConfiguration> configure)
        {
            builder.AddChannelLogger();
            builder.Services.Configure(configure);

            return builder;
        }
    }
}
