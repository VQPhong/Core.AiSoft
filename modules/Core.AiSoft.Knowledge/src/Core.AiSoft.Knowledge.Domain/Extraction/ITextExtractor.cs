using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Core.AiSoft.Knowledge.Extraction;

/// <summary>
/// Turns the bytes of one file format into normalized text sections. Implementations live in the infrastructure layer
/// (they depend on format libraries); the ingestion pipeline picks the one whose <see cref="CanExtract"/> matches.
/// </summary>
public interface ITextExtractor
{
    bool CanExtract(string extension);

    Task<ExtractedDocument> ExtractAsync(Stream content, CancellationToken cancellationToken = default);
}
