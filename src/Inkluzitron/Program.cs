using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Inkluzitron.Handlers;
using Inkluzitron.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Inkluzitron
{
    static public class Program
    {
        static public Task Main(string[] args) => MainAsync(args);

        static private async Task MainAsync(string[] args)
        {
            var closeAppToken = new CancellationTokenSource();

            Console.CancelKeyPress += (_, e) =>
            {
                // Handles CTRL+C (SIGINT) signals.
                e.Cancel = true;
                closeAppToken.Cancel();
            };

            var configuration = BuildConfiguration(args);

            var discordConfig = new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Verbose,
                MessageCacheSize = 100000,
                RateLimitPrecision = RateLimitPrecision.Millisecond
            };

            var commandsConfig = new CommandServiceConfig()
            {
                LogLevel = LogSeverity.Verbose,
                CaseSensitiveCommands = true,
                DefaultRunMode = RunMode.Async
            };

            var services = new ServiceCollection()
                .AddSingleton(new DiscordSocketClient(discordConfig))
                .AddSingleton(new CommandService(commandsConfig))
                .AddSingleton(configuration)
                .AddSingleton<RuntimeService>()
                .AddSingleton<LoggingService>()
                .AddSingleton<Random>();

            services.AddLogging(config =>
            {
                config
                    .SetMinimumLevel(LogLevel.Information)
                    .AddSystemdConsole(opt =>
                    {
                        opt.IncludeScopes = true;
                        opt.TimestampFormat = "dd. MM. yyyy HH:mm:ss\t";
                    });
            });

            var handlers = Assembly.GetExecutingAssembly().GetTypes()
                .Where(o => o.GetInterface(nameof(IHandler)) != null)
                .ToList();

            handlers.ForEach(handler => services.AddSingleton(handler));

            var provider = services.BuildServiceProvider();

            provider.GetRequiredService<LoggingService>();
            handlers.ForEach(o => provider.GetRequiredService(o));

            var runtime = provider.GetRequiredService<RuntimeService>();
            await runtime.StartAsync();

            try
            {
                await Task.Delay(-1, closeAppToken.Token);
            }
            catch (TaskCanceledException)
            {
                // Can ignore
            }

            await runtime.StopAsync();
            provider.Dispose();
        }

        static private IConfiguration BuildConfiguration(string[] args)
        {
            var builder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json");

            var env = Environment.GetEnvironmentVariable("ENVIRONMENT");
            if (!string.IsNullOrEmpty(env))
                builder.AddJsonFile($"appsettings.{env.Trim()}.json", true);

            return builder
                .AddCommandLine(args)
                .AddEnvironmentVariables()
                .Build();
        }
    }
}
