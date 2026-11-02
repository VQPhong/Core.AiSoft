using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Core.AiSoft.Knowledge.Extraction;

public partial class MarkdownTextExtractor : Utf8TextExtractorBase
{
    protected override IReadOnlyCollection<string> SupportedExtensions { get; } = [".md"];

    protected override List<ExtractedSection> SplitSections(string text)
    {
        var sections = new List<ExtractedSection>();
        var headings = new string?[6];
        var body = new StringBuilder();
        string? openFence = null;

        foreach (var line in text.Replace("\r\n", "\n").Split('\n'))
        {
            var fence = FenceRegex().Match(line);
            if (fence.Success)
            {
                var marker = fence.Groups["marker"].Value;
                if (openFence == null)
                {
                    openFence = marker;
                }
                else if (marker[0] == openFence[0] && marker.Length >= openFence.Length)
                {
                    openFence = null;
                }
            }

            var heading = openFence == null ? HeadingRegex().Match(line) : Match.Empty;
            if (!heading.Success)
            {
                body.Append(line).Append('\n');
                continue;
            }

            Flush();

            var level = heading.Groups["hashes"].Value.Length;
            headings[level - 1] = heading.Groups["title"].Value;
            for (var i = level; i < headings.Length; i++)
            {
                headings[i] = null;
            }
        }

        Flush();
        return sections;

        void Flush()
        {
            sections.Add(new ExtractedSection(headings.Where(h => h != null).Select(h => h!).ToList(), body.ToString()));
            body.Clear();
        }
    }

    /// <summary>
    /// Markdown regex for heading. By default, Markdown support up to 6 heading level
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex(@"^ {0,3}(?<hashes>#{1,6})[ \t]+(?<title>.*?)(?:[ \t]+#+)?[ \t]*$")]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"^ {0,3}(?<marker>`{3,}|~{3,})")]
    private static partial Regex FenceRegex();
}
