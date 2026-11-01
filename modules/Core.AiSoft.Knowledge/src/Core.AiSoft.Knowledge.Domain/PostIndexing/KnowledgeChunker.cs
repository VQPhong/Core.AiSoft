using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Core.AiSoft.Knowledge.Extraction;

namespace Core.AiSoft.Knowledge.PostIndexing;

public static partial class KnowledgeChunker
{
    public static IReadOnlyList<KnowledgeChunk> Split(ExtractedDocument document, KnowledgeChunkingOptions options)
    {
        var maxLength = Math.Max(options.MaxLength, KnowledgeChunkingOptions.MinMaxLength);
        var overlapLength = Math.Clamp(options.OverlapLength, 0, maxLength / 2);

        var chunks = new List<KnowledgeChunk>();

        foreach (var section in document.Sections)
        {
            var text = section.Text.Trim();
            if (text.Length == 0)
            {
                continue;
            }

            string? previous = null;
            foreach (var piece in SplitText(text, maxLength - overlapLength - 1))
            {
                var overlap = previous == null ? "" : Tail(previous, overlapLength);
                var chunkText = overlap.Length == 0 ? piece : $"{overlap} {piece}";

                chunks.Add(new KnowledgeChunk(chunks.Count, section.HeadingPath, chunkText, section.PageNumber));
                previous = piece;
            }
        }

        return chunks;
    }

    public static IReadOnlyList<string> SplitText(string text, int limit)
    {
        var pieces = new List<string>();
        Pack(SplitParagraphs(text), level: 0, limit, pieces);
        return pieces;
    }

    private static void Pack(IEnumerable<string> units, int level, int limit, List<string> pieces)
    {
        var buffer = new StringBuilder();

        foreach (var unit in units)
        {
            if (buffer.Length + unit.TrimEnd().Length <= limit)
            {
                buffer.Append(unit);
                continue;
            }

            Flush();

            if (unit.TrimEnd().Length <= limit)
            {
                buffer.Append(unit);
            }
            else if (level < Boundaries.Length)
            {
                Pack(SplitAfter(unit, Boundaries[level]), level + 1, limit, pieces);
            }
            else
            {
                var trimmed = unit.Trim();
                for (var start = 0; start < trimmed.Length; start += limit)
                {
                    pieces.Add(trimmed.Substring(start, Math.Min(limit, trimmed.Length - start)));
                }
            }
        }

        Flush();

        void Flush()
        {
            var piece = buffer.ToString().Trim();
            if (piece.Length > 0)
            {
                pieces.Add(piece);
            }

            buffer.Clear();
        }
    }

    private static IEnumerable<string> SplitParagraphs(string text)
    {
        var paragraph = new StringBuilder();
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

            if (openFence == null && string.IsNullOrWhiteSpace(line))
            {
                if (paragraph.Length > 0)
                {
                    yield return paragraph.Append('\n').ToString();
                    paragraph.Clear();
                }

                continue;
            }

            paragraph.Append(line).Append('\n');
        }

        if (paragraph.Length > 0)
        {
            yield return paragraph.ToString();
        }
    }

    private static IEnumerable<string> SplitAfter(string text, Regex boundary)
    {
        var start = 0;
        foreach (Match match in boundary.Matches(text))
        {
            var end = match.Index + match.Length;
            if (end > start && end < text.Length)
            {
                yield return text[start..end];
                start = end;
            }
        }

        yield return text[start..];
    }

    public static string Tail(string text, int maxLength)
    {
        if (maxLength <= 0)
        {
            return "";
        }

        if (text.Length <= maxLength)
        {
            return text;
        }

        var minStart = text.Length - maxLength;

        foreach (var boundary in TailBoundaries)
        {
            var start = boundary.Matches(text)
                .Select(m => m.Index + m.Length)
                .Where(end => end >= minStart && end < text.Length)
                .DefaultIfEmpty(-1)
                .Min();

            if (start >= 0)
            {
                return text[start..].Trim();
            }
        }

        return "";
    }

    private static readonly Regex[] Boundaries =
    [
        LineBreakRegex(),
        SentenceEndRegex(),
        ClauseEndRegex(),
        WhitespaceRegex()
    ];

    private static readonly Regex[] TailBoundaries =
    [
        SentenceEndRegex(),
        ClauseEndRegex(),
        WhitespaceRegex()
    ];

    [GeneratedRegex(@"\n")]
    private static partial Regex LineBreakRegex();

    // Punctuation followed by whitespace, so "3.5" or "v1.2" are not cut.
    [GeneratedRegex(@"[.!?…。！？]+[""'”’)\]]*\s+")]
    private static partial Regex SentenceEndRegex();

    [GeneratedRegex(@"[,;:，；：]\s+")]
    private static partial Regex ClauseEndRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"^ {0,3}(?<marker>`{3,}|~{3,})")]
    private static partial Regex FenceRegex();
}
