using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.AiSoft.Knowledge.Extraction;

public abstract class Utf8TextExtractorBase : TextExtractorBase
{
    protected override async Task<List<ExtractedSection>> ExtractSectionsAsync(Stream content, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var text = await reader.ReadToEndAsync(cancellationToken);

        return SplitSections(text);
    }

    protected abstract List<ExtractedSection> SplitSections(string text);
}
