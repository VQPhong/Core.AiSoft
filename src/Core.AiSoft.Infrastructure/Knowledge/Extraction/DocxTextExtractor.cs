using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Core.AiSoft.Knowledge.Extraction;

// TODO: add header extractor for non-heading headers
public class DocxTextExtractor : TextExtractorBase
{
    private const int MaxHeadingLevel = 9;

    protected override IReadOnlyCollection<string> SupportedExtensions { get; } = [".docx"];

    protected override Task<List<ExtractedSection>> ExtractSectionsAsync(Stream content, CancellationToken cancellationToken)
    {
        using var document = WordprocessingDocument.Open(content, isEditable: false);

        var mainPart = document.MainDocumentPart
                       ?? throw new InvalidDataException("The document has no main part.");
        var body = mainPart.Document?.Body
                   ?? throw new InvalidDataException("The document has no body.");

        var walker = new Walker(BuildStyleLevels(mainPart.StyleDefinitionsPart?.Styles));
        walker.Visit(body.ChildElements, cancellationToken);

        return Task.FromResult(walker.Finish());
    }

    private static Dictionary<string, int> BuildStyleLevels(Styles? styles)
    {
        var levels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (styles == null)
        {
            return levels;
        }

        var paragraphStyles = styles.Elements<Style>()
            .Where(s => s.StyleId?.Value != null && (s.Type == null || s.Type.Value == StyleValues.Paragraph))
            .ToDictionary(s => s.StyleId!.Value!, StringComparer.OrdinalIgnoreCase);

        foreach (var styleId in paragraphStyles.Keys)
        {
            var level = ResolveLevel(styleId, paragraphStyles, depth: 0);
            if (level > 0)
            {
                levels[styleId] = level;
            }
        }

        return levels;
    }

    private static int ResolveLevel(string styleId, Dictionary<string, Style> styles, int depth)
    {
        if (depth > 10 || !styles.TryGetValue(styleId, out var style))
        {
            return 0;
        }

        var name = style.StyleName?.Val?.Value;
        if (name != null
            && name.StartsWith("heading ", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(name["heading ".Length..], out var fromName))
        {
            return Math.Clamp(fromName, 1, MaxHeadingLevel);
        }

        var outline = style.StyleParagraphProperties?.OutlineLevel?.Val?.Value;
        if (outline is >= 0 and < MaxHeadingLevel)
        {
            return outline.Value + 1;
        }

        var basedOn = style.BasedOn?.Val?.Value;
        return basedOn == null ? 0 : ResolveLevel(basedOn, styles, depth + 1);
    }

    private sealed class Walker
    {
        private readonly Dictionary<string, int> _styleLevels;
        private readonly List<ExtractedSection> _sections = [];
        private readonly string?[] _headings = new string?[MaxHeadingLevel];
        private readonly StringBuilder _body = new();

        public Walker(Dictionary<string, int> styleLevels)
        {
            _styleLevels = styleLevels;
        }

        public void Visit(IEnumerable<OpenXmlElement> elements, CancellationToken cancellationToken)
        {
            foreach (var element in elements)
            {
                cancellationToken.ThrowIfCancellationRequested();

                switch (element)
                {
                    case Paragraph paragraph:
                        VisitParagraph(paragraph);
                        break;
                    case Table table:
                        VisitTable(table);
                        break;
                    case SdtBlock contentControl:
                        Visit(contentControl.SdtContentBlock?.ChildElements ?? Enumerable.Empty<OpenXmlElement>(), cancellationToken);
                        break;
                }
            }
        }

        public List<ExtractedSection> Finish()
        {
            Flush();
            return _sections;
        }

        private void VisitParagraph(Paragraph paragraph)
        {
            var text = GetText(paragraph);
            var level = GetHeadingLevel(paragraph);

            if (level == 0 || string.IsNullOrWhiteSpace(text))
            {
                _body.Append(text).Append('\n');
                return;
            }

            Flush();
            _headings[level - 1] = text;
            for (var i = level; i < _headings.Length; i++)
            {
                _headings[i] = null;
            }
        }

        private void VisitTable(Table table)
        {
            foreach (var row in table.Elements<TableRow>())
            {
                var cells = row.Elements<TableCell>()
                    .Select(cell => string.Join(" ", cell.Elements<Paragraph>().Select(GetText)).Trim());

                _body.Append(string.Join(" | ", cells)).Append('\n');
            }

            _body.Append('\n');
        }

        private int GetHeadingLevel(Paragraph paragraph)
        {
            var properties = paragraph.ParagraphProperties;

            var outline = properties?.OutlineLevel?.Val?.Value;
            if (outline is >= 0 and < MaxHeadingLevel)
            {
                return outline.Value + 1;
            }

            var styleId = properties?.ParagraphStyleId?.Val?.Value;
            return styleId != null && _styleLevels.TryGetValue(styleId, out var level) ? level : 0;
        }

        private void Flush()
        {
            _sections.Add(new ExtractedSection(_headings.Where(h => h != null).Select(h => h!).ToList(), _body.ToString()));
            _body.Clear();
        }

        private static string GetText(Paragraph paragraph)
        {
            var builder = new StringBuilder();
            foreach (var element in paragraph.Descendants())
            {
                switch (element)
                {
                    case Text text:
                        builder.Append(text.Text);
                        break;
                    case TabChar:
                        builder.Append('\t');
                        break;
                    case Break or CarriageReturn:
                        builder.Append('\n');
                        break;
                }
            }

            return builder.ToString();
        }
    }
}
