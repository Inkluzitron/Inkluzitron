using Inkluzitron.Contracts;
using Inkluzitron.Data;
using Inkluzitron.Data.Entities;
using Inkluzitron.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
        private Lazy<IScheduledTaskHandler[]> Handlers { get; }
        private ILogger Logger { get; }

        private ManualResetEvent ScheduledTaskExists { get; } = new(false);
        private CancellationTokenSource CancellationTokenSource { get; set; }
        private Task Worker { get; set; }

        private bool IsStarted { get; set; }
        StartCondition ILifecycleControl.WhenToStart => StartCondition.WhenHomeGuildBecomesAvailable;
        bool ILifecycleControl.IsStarted => IsStarted;

        public ScheduledTasksService(DatabaseFactory databaseFactory, IServiceProvider serviceProvider, ILogger<ScheduledTasksService> logger)
        {
            DbFactory = databaseFactory;
            Handlers = new Lazy<IScheduledTaskHandler[]>(() => serviceProvider.GetServices<IScheduledTaskHandler>().ToArray());
            Logger = logger;
        }


        Task ILifecycleControl.StartAsync()
        {
            IsStarted = true;
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
            IsStarted = false;
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

        public async Task<ScheduledTask> LookupAsync(int taskId)
        {
            using var dbContext = DbFactory.Create();
            return await dbContext.ScheduledTasks.FindAsync(taskId);
        }

        public async Task<IReadOnlyCollection<ScheduledTask>> LookupAsync(string discriminator, string tag)
        {
            using var dbContext = DbFactory.Create();
            return await dbContext.ScheduledTasks.AsQueryable()
                .Where(t => t.Discriminator == discriminator && t.Tag == tag)
                .ToListAsync();
        }

        public async Task<bool> CancelAsync(long scheduledTaskId)
        {
            using var dbContext = DbFactory.Create();
            var scheduledTask = await dbContext.ScheduledTasks.FindAsync(scheduledTaskId);
            if (scheduledTask == null)
                return false;

            dbContext.ScheduledTasks.Remove(scheduledTask);
            await dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<int> CancelAsync(string discriminator, string tag)
        {
            using var dbContext = DbFactory.Create();
            var results = await dbContext.ScheduledTasks.AsQueryable()
                .Where(t => t.Discriminator == discriminator && t.Tag == tag)
                .ToListAsync();

            dbContext.RemoveRange(results);
            await dbContext.SaveChangesAsync();

            return results.Count;
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

        private async Task<Task> CreateWaitForNextIterationAsync(BotDatabaseContext dbContext, CancellationToken cancellationToken)
        {
            var nextScheduledTask = await dbContext.ScheduledTasks.AsQueryable()
                .OrderBy(st => st.MsSinceUtcUnixEpoch)
                .FirstOrDefaultAsync(cancellationToken);

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
            var now = DateTimeOffset.UtcNow.ConvertDateTimeOffsetToLong();
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
                foreach (var handler in Handlers.Value)
                {
                    var handled = await handler.HandleAsync(scheduledTask);
                    if (handled)
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
