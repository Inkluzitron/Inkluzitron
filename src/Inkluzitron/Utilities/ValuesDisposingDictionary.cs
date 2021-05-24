using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Inkluzitron.Utilities
{
    [Serializable]
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

        public ValuesDisposingDictionary()
        {
        }

        protected ValuesDisposingDictionary(SerializationInfo serializationInfo, StreamingContext streamingContext) : base(serializationInfo, streamingContext)
        {
        }
    }
}
