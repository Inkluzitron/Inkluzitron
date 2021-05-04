using System.Collections.Generic;

namespace Inkluzitron.Contracts
{
    /// <summary>
    /// Can be used with the embed builder extension methods defined in <see cref="Inkluzitron.Extensions.EmbedMetadataExtensions"/>
    /// to store data inside embeds.
    /// </summary>
    /// <seealso cref="Inkluzitron.Modules.BdsmTestOrg.QuizEmbedMetadata"/>
    public interface IEmbedMetadata
    {
        string EmbedKind { get; }
        bool TryLoadFrom(IReadOnlyDictionary<string, string> values);
        void SaveInto(IDictionary<string, string> destination);
    }
}
