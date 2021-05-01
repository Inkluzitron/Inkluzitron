using System.Collections.Generic;

namespace Inkluzitron.Contracts
{
    public interface IEmbedMetadata
    {
        string EmbedKind { get; }
        bool TryLoadFrom(IReadOnlyDictionary<string, string> values);
        void SaveInto(IDictionary<string, string> destination);
    }
}
