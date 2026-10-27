using System;
using System.Collections.Generic;

namespace Core.AiSoft.Knowledge.Extraction;

public class PlainTextExtractor : Utf8TextExtractorBase
{
    protected override IReadOnlyCollection<string> SupportedExtensions { get; } = [".txt"];

    protected override List<ExtractedSection> SplitSections(string text)
    {
        return [new ExtractedSection(Array.Empty<string>(), text)];
    }
}
