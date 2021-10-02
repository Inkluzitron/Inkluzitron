using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Inkluzitron.Contracts;
using Inkluzitron.Data;
using Inkluzitron.Extensions;
using Inkluzitron.Handlers;
using Inkluzitron.Models.Settings;
using Inkluzitron.Modules;
using Inkluzitron.Modules.BdsmTestOrg;
using Inkluzitron.Modules.Points;
using Inkluzitron.Modules.Vote;
using Inkluzitron.Services;
using Inkluzitron.Services.ChannelLogger;
using Inkluzitron.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Data.Common;
using System.Linq;
using System.Net.Http.Headers;
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
                .AddSingleton<GraphPaintingService>()
                .AddSingleton<UserBdsmTraitsService>()
                .AddSingleton<BdsmTraitOperationCheckTranslations>()
                .AddSingleton<ImagesService>()
                .AddSingleton<SendSettings>()
                .AddSingleton<BotSettings>()
                .AddSingleton(new FileCache(cacheDirLocation))
                .AddSingleton<PointsService>()
                .AddSingleton<PointsGraphPaintingStrategy>()
                .AddSingleton<BdsmGraphPaintingStrategy>()
                .AddSingleton<UsersService>()
                .AddSingleton<KisSettings>()
                .AddSingleton<FamilyTreeService>()
                .AddSingletonWithInterface<ScheduledTasksService, IRuntimeEventHandler>()
                .AddVoteModule()
                .AddSingleton<BirthdaySettings>()
                .AddSingletonWithInterface<BirthdayNotificationService, IScheduledTaskHandler>()
                .AddHttpClient()
                .AddMemoryCache()
                .AddLogging(config =>
                {
                    config.AddConfiguration(configuration.GetSection("Logging"))
                        .AddConsole()
                        .AddChannelLogger();
                })
                .AddSingleton<KisService>();

            var handlers = Assembly.GetExecutingAssembly().GetTypes()
                .Where(o => o.GetInterface(nameof(IHandler)) != null)
                .ToList();

            handlers.ForEach(handler => services.RegisterAs(handler, typeof(IHandler)));

            Assembly.GetExecutingAssembly().GetTypes()
                .Where(o => o.GetInterface(nameof(IReactionHandler)) != null)
                .ToList()
                .ForEach(reactionHandlerType => services.RegisterAs(reactionHandlerType, typeof(IReactionHandler)));

            if (!string.IsNullOrEmpty(configuration["Kis:Token"]))
            {
                services.AddHttpClient("Kis", c =>
                {
                    c.BaseAddress = new Uri(configuration["Kis:Api"]);
                    c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", configuration["Kis:Token"]);
                });
            }

            var provider = services.BuildServiceProvider();

            provider.GetRequiredService<LoggingService>();
            handlers.ForEach(o => provider.GetRequiredService(o));

            var context = provider.GetRequiredService<BotDatabaseContext>();
            await context.Database.MigrateAsync();

            provider.GetRequiredService<ReactionsModule>();
            provider.GetRequiredService<PointsService>();

            var runtimeService = provider.GetRequiredService<RuntimeService>();
            await runtimeService.StartAsync();

            foreach (var runtimeEventHandler in provider.GetServices<IRuntimeEventHandler>())
                await runtimeEventHandler.OnBotStartingAsync();

            try
            {
                await Task.Delay(-1, closeAppToken.Token);
            }
            catch (TaskCanceledException)
            {
                // Can ignore
            }

            foreach (var runtimeEventHandler in provider.GetServices<IRuntimeEventHandler>().Reverse())
                await runtimeEventHandler.OnBotStoppingAsync();

            await runtimeService.StopAsync();
            await provider.DisposeAsync();
        }

        static public IConfiguration BuildConfiguration(string[] args)
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

        static public string BuildConnectionString(string sqliteDatabasePath)
        {
            var builder = new DbConnectionStringBuilder()
            {
                { "Data Source", sqliteDatabasePath }
            };

            return builder.ConnectionString;
        }
    }
}
