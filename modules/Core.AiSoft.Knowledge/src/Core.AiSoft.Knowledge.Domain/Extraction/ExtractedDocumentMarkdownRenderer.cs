using System;
using System.Collections.Generic;
using System.Text;

namespace Core.AiSoft.Knowledge.Extraction;

public static class ExtractedDocumentMarkdownRenderer
{
    private const int MaxMarkdownHeadingLevel = 6;

    public static string Render(ExtractedDocument document)
    {
        var markdown = new StringBuilder();
        IReadOnlyList<string> previousPath = Array.Empty<string>();
        int? previousPage = null;

        foreach (var section in document.Sections)
        {
            if (section.PageNumber.HasValue && section.PageNumber != previousPage)
            {
                AppendBlock(markdown, $"<!-- page:{section.PageNumber} -->");
                previousPage = section.PageNumber;
            }

            var firstChanged = FirstDifferentIndex(previousPath, section.HeadingPath);
            for (var level = firstChanged; level < section.HeadingPath.Count; level++)
            {
                var hashes = new string('#', Math.Min(level + 1, MaxMarkdownHeadingLevel));
                AppendBlock(markdown, $"{hashes} {section.HeadingPath[level]}");
            }

            previousPath = section.HeadingPath;
            AppendBlock(markdown, section.Text);
        }

        return markdown.ToString().TrimEnd() + "\n";
    }

    private static int FirstDifferentIndex(IReadOnlyList<string> previous, IReadOnlyList<string> current)
    {
        var index = 0;
        while (index < previous.Count && index < current.Count && previous[index] == current[index])
        {
            index++;
        }

        return index;
    }

    private static void AppendBlock(StringBuilder markdown, string block)
    {
        if (markdown.Length > 0)
        {
            markdown.Append('\n');
        }

        markdown.Append(block.TrimEnd()).Append('\n');
    }
}
