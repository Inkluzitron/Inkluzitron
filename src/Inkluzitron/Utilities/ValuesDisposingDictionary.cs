using System;
using System.Collections.Generic;

namespace Inkluzitron.Utilities
{
    public class ValuesDisposingDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IDisposable
        where TValue : IDisposable
    {
        public void Dispose()
        {
            foreach (var value in Values)
                value?.Dispose();

            Clear();
            GC.SuppressFinalize(this);
        }
    }
}
