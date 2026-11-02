using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using Volo.Abp;

namespace Core.AiSoft.Knowledge.Extraction;

public class PdfTextExtractor : TextExtractorBase
{
    public const int MinLettersPerPage = 20;

    protected override IReadOnlyCollection<string> SupportedExtensions { get; } = [".pdf"];

    protected override Task<List<ExtractedSection>> ExtractSectionsAsync(Stream content, CancellationToken cancellationToken)
    {
        using var pdf = PdfDocument.Open(content);

        var sections = new List<ExtractedSection>(pdf.NumberOfPages);
        var letters = 0L;

        foreach (var page in pdf.GetPages())
        {
            cancellationToken.ThrowIfCancellationRequested();

            letters += page.Letters.Count(l => !string.IsNullOrWhiteSpace(l.Value));

            sections.Add(new ExtractedSection([], ContentOrderTextExtractor.GetText(page), page.Number));
        }

        if (pdf.NumberOfPages > 0 && letters / pdf.NumberOfPages < MinLettersPerPage)
        {
            throw new BusinessException(KnowledgeErrorCodes.PdfHasNoTextLayer);
        }

        return Task.FromResult(sections);
    }
}
