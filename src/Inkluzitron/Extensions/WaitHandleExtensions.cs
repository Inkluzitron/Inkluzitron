using System;
using System.Threading;
using System.Threading.Tasks;

namespace Inkluzitron.Extensions
{
    static public class WaitHandleExtensions
    {
        // https://docs.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/interop-with-other-asynchronous-patterns-and-types
        static public Task WaitOneAsync(this WaitHandle waitHandle, CancellationToken cancellationToken)
        {
            if (waitHandle == null)
                throw new ArgumentNullException("waitHandle");

            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));
            var rwh = ThreadPool.RegisterWaitForSingleObject(waitHandle,
                delegate { tcs.TrySetResult(true); }, null, -1, true);
            var t = tcs.Task;
            t.ContinueWith(_ => rwh.Unregister(null));
            return t;
        }
    }
}
