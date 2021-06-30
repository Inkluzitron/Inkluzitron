using Inkluzitron.Contracts;
using Inkluzitron.Data;
using Inkluzitron.Data.Entities;
using Inkluzitron.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Inkluzitron.Services
{
    public sealed class ScheduledTasksService : ILifecycleControl, IDisposable
    {
        private const int FailCountThreshold = 5;

        private DatabaseFactory DbFactory { get; }
        private IScheduledTaskHandler[] Handlers { get; }
        private ILogger Logger { get; }

        private ManualResetEvent ScheduledTaskExists { get; } = new(false);
        private CancellationTokenSource CancellationTokenSource { get; set; }
        private Task Worker { get; set; }

        public ScheduledTasksService(DatabaseFactory databaseFactory, IEnumerable<IScheduledTaskHandler> handlers, ILogger<ScheduledTasksService> logger)
        {
            DbFactory = databaseFactory;
            Handlers = handlers.ToArray();
            Logger = logger;
        }

        Task ILifecycleControl.StartAsync()
        {
            CancellationTokenSource = new CancellationTokenSource();
            Worker = Task.Factory.StartNew(
                () => ProcessScheduledTasksAsync(CancellationTokenSource.Token),
                CancellationTokenSource.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default
            );
            return Task.CompletedTask;
        }

        async Task ILifecycleControl.StopAsync()
        {
            CancellationTokenSource.Cancel();
            CancellationTokenSource.Dispose();
            await Worker.ContinueWith(_ => { });
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            CancellationTokenSource.Dispose();
            ScheduledTaskExists.Dispose();
        }

        public async Task EnqueueAsync(ScheduledTask scheduledTask)
        {
            using var dbContext = DbFactory.Create();
            dbContext.ScheduledTasks.Add(scheduledTask);
            await dbContext.SaveChangesAsync();
            ScheduledTaskExists.Set();
        }

        private async Task ProcessScheduledTasksAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                Logger.LogDebug("Processing scheduled tasks.");

                ScheduledTaskExists.Reset();
                var untilNextIteration = Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);

                try
                {
                    using var dbContext = DbFactory.Create();
                    await FindAndProcessPendingTasks(dbContext, cancellationToken);

                    untilNextIteration = await CreateWaitForNextIterationAsync(dbContext, cancellationToken);
                    untilNextIteration = untilNextIteration.ContinueWith(_ => {}, cancellationToken);
                }
                catch (OperationCanceledException e) when (e.CancellationToken == cancellationToken)
                {
                    break;
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Error processing scheduled tasks");
                }

                await untilNextIteration;
            }
        }

        private Task<Task> CreateWaitForNextIterationAsync(BotDatabaseContext dbContext, CancellationToken cancellationToken)
        {
            var nextScheduledTask = dbContext.ScheduledTasks.AsQueryable()
                .OrderBy(st => st.MsSinceUtcUnixEpoch)
                .FirstOrDefault();

            Task newItemAdded = ScheduledTaskExists.WaitOneAsync(cancellationToken);
            Task nextItemTick = Task.Delay(Timeout.Infinite, cancellationToken);

            if (nextScheduledTask is not null)
            {
                var timeLeft = nextScheduledTask.When - DateTimeOffset.UtcNow;
                if (timeLeft.TotalSeconds < 0)
                    timeLeft = TimeSpan.Zero;

                nextItemTick = Task.Delay(timeLeft, cancellationToken);
            }

            return Task.WhenAny(newItemAdded, nextItemTick);
        }

        private async Task FindAndProcessPendingTasks(BotDatabaseContext dbContext, CancellationToken cancellationToken)
        {
            var now = ScheduledTask.ConvertDateTimeOffset(DateTimeOffset.UtcNow);
            var pendingTasks = dbContext.ScheduledTasks.AsQueryable()
                .OrderBy(st => st.MsSinceUtcUnixEpoch)
                .Where(st => st.MsSinceUtcUnixEpoch <= now)
                .ToAsyncEnumerable();

            await foreach (var pendingTask in pendingTasks)
            {
                if (await HandleScheduledTask(pendingTask))
                    dbContext.ScheduledTasks.Remove(pendingTask);

                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        private async Task<bool> HandleScheduledTask(ScheduledTask scheduledTask)
        {
            try
            {
                foreach (var handler in Handlers)
                {
                    var taskWasHandled = await handler.TryHandleAsync(scheduledTask);
                    if (taskWasHandled)
                        return true;
                }

                Logger.LogError("A scheduled task was refused for handling by all known handlers, postponing: {0}", scheduledTask.Serialize());
                scheduledTask.When += TimeSpan.FromHours(1);
            }
            catch (Exception e)
            {
                scheduledTask.FailCount++;
                Logger.LogError(e, "Error handling scheduled task (attempt {0}/{1}: {2}", scheduledTask.FailCount, FailCountThreshold, scheduledTask.Serialize());

                if (scheduledTask.FailCount >= FailCountThreshold)
                    return true;
                else
                    scheduledTask.When = DateTimeOffset.UtcNow.AddMinutes(1);
            }

            return false;
        }
    }
}
