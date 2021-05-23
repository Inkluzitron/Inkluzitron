using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Inkluzitron.Contracts;
using Inkluzitron.Data;
using Inkluzitron.Handlers;
using Inkluzitron.Models.Settings;
using Inkluzitron.Modules;
using Inkluzitron.Modules.BdsmTestOrg;
using Inkluzitron.Services;
using Inkluzitron.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Data.Common;
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

            var dbFileLocation = configuration.GetValue<string>("DatabaseFilePath");
            if (dbFileLocation == null)
                throw new InvalidOperationException("The 'DatabaseFilePath' configuration value is missing.");

            var cacheDirLocation = configuration.GetValue<string>("CacheDirectoryPath");
            if (cacheDirLocation == null)
                throw new InvalidOperationException("The 'CacheDirectoryPath' configuration value is missing.");

            var services = new ServiceCollection()
                .AddSingleton(new DiscordSocketClient(discordConfig))
                .AddSingleton(new CommandService(commandsConfig))
                .AddSingleton(configuration)
                .AddSingleton<RuntimeService>()
                .AddSingleton<LoggingService>()
                .AddDbContext<BotDatabaseContext>(c => c.UseSqlite(BuildConnectionString(dbFileLocation)))
                .AddSingleton<DatabaseFactory>()
                .AddSingleton<ReactionSettings>()
                .AddSingleton<BdsmTestOrgSettings>()
                .AddSingleton<ReactionsModule>()
                .AddSingleton<FontService>()
                .AddSingleton<GraphPaintingService>()
                .AddSingleton<UserBdsmTraitsService>()
                .AddSingleton<BdsmTraitOperationCheckTranslations>()
                .AddSingleton<ImagesService>()
                .AddSingleton(new FileCache(cacheDirLocation))
                .AddHttpClient()
                .AddMemoryCache()
                .AddLogging(config =>
                {
                    config.AddConfiguration(configuration.GetSection("Logging"));
                    config.AddSystemdConsole(opt =>
                    {
                        opt.IncludeScopes = true;
                        opt.TimestampFormat = "dd. MM. yyyy HH:mm:ss\t";
                    });
                });

            var handlers = Assembly.GetExecutingAssembly().GetTypes()
                .Where(o => o.GetInterface(nameof(IHandler)) != null)
                .ToList();

            handlers.ForEach(handler => services.AddSingleton(handler));

            Assembly.GetExecutingAssembly().GetTypes()
                .Where(o => o.GetInterface(nameof(IReactionHandler)) != null)
                .ToList()
                .ForEach(reactionHandlerType => services.AddSingleton(typeof(IReactionHandler), reactionHandlerType));

            var provider = services.BuildServiceProvider();

            provider.GetRequiredService<LoggingService>();
            handlers.ForEach(o => provider.GetRequiredService(o));

            var context = provider.GetRequiredService<BotDatabaseContext>();
            await context.Database.MigrateAsync();

            provider.GetRequiredService<ReactionsModule>();

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
            await provider.DisposeAsync();
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

        static private string BuildConnectionString(string sqliteDatabasePath)
        {
            var builder = new DbConnectionStringBuilder()
            {
                { "Data Source", sqliteDatabasePath }
            };

            return builder.ConnectionString;
        }
    }
}
