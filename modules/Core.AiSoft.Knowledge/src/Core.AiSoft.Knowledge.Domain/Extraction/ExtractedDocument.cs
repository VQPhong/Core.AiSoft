using System.Collections.Generic;
using System.Linq;

namespace Core.AiSoft.Knowledge.Extraction;

public class ExtractedDocument
{
    public IReadOnlyList<ExtractedSection> Sections { get; }

    public int CharacterCount => Sections.Sum(s => s.Text.Length);

    public bool HasHeadings => Sections.Any(s => s.HeadingPath.Count > 0);

    public ExtractedDocument(IReadOnlyList<ExtractedSection> sections)
    {
        Sections = sections;
    }
}

public record ExtractedSection(IReadOnlyList<string> HeadingPath, string Text, int? PageNumber = null)
{
    public int Level => HeadingPath.Count;
}
