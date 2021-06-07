using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Inkluzitron.Utilities
{
    static public class Patiently
    {
        private const int Patience = 10;
        static private readonly TimeSpan BackOffDelay = TimeSpan.FromMilliseconds(200);

        static public async Task HandleDbConcurrency(Func<Task> concurrentDbTaskFunc)
        {
            if (concurrentDbTaskFunc is null)
                throw new ArgumentNullException(nameof(concurrentDbTaskFunc));

            for (var attemptNumber = 1; attemptNumber <= Patience; attemptNumber++)
            {
                try
                {
                    await concurrentDbTaskFunc();
                    return;
                }
                catch (DbUpdateConcurrencyException) when (attemptNumber != Patience)
                {
                    await Task.Delay(BackOffDelay);
                }
            }
        }

        static public async Task<T> HandleDbConcurrency<T>(Func<Task<T>> concurrentDbTaskFunc)
        {
            if (concurrentDbTaskFunc is null)
                throw new ArgumentNullException(nameof(concurrentDbTaskFunc));

            for (var attemptNumber = 1; attemptNumber <= Patience; attemptNumber++)
            {
                try
                {
                    return await concurrentDbTaskFunc();
                }
                catch (DbUpdateConcurrencyException) when (attemptNumber != Patience)
                {
                    await Task.Delay(BackOffDelay);
                }
            }

            throw new InvalidOperationException();
        }
    }
}
