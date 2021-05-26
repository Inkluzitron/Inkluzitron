using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace Inkluzitron.Utilities
{
    public class Patiently
    {
        private const int Patience = 10;
        private static readonly TimeSpan BackOffDelay = TimeSpan.FromMilliseconds(200);

        public static async Task HandleDbConcurrency(Func<Task> concurrentDbTaskFunc)
        {
            if (concurrentDbTaskFunc is null)
                throw new ArgumentNullException(nameof(concurrentDbTaskFunc));

            var attemptNumber = 1;

            while (true)
            {
                try
                {
                    await concurrentDbTaskFunc();
                    return;
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (attemptNumber == Patience)
                        throw;

                    attemptNumber++;
                    await Task.Delay(BackOffDelay);
                }
            }
        }
    }
}
