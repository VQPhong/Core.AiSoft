using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Core.AiSoft.Knowledge.Extraction;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace Core.AiSoft.Knowledge.Extraction;

public abstract partial class TextExtractorBase : ITextExtractor, ITransientDependency
{
    protected abstract IReadOnlyCollection<string> SupportedExtensions { get; }

    protected string PrimaryExtension => SupportedExtensions.First();

    public virtual bool CanExtract(string extension)
    {
        return SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
    }

    public virtual async Task<ExtractedDocument> ExtractAsync(Stream content, CancellationToken cancellationToken = default)
    {
        Check.NotNull(content, nameof(content));

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        List<ExtractedSection> sections;
        try
        {
            sections = await ExtractSectionsAsync(buffer, cancellationToken);
        }
        catch (Exception ex) when (ex is not BusinessException and not OperationCanceledException)
        {
            throw new BusinessException(KnowledgeErrorCodes.FileUnreadable, innerException: ex)
                .WithData("extension", PrimaryExtension);
        }

        var normalized = sections
            .Select(s => s with
            {
                HeadingPath = s.HeadingPath.Select(NormalizeHeading).Where(h => h.Length > 0).ToList(),
                Text = NormalizeText(s.Text)
            })
            .Where(s => s.Text.Length > 0)
            .ToList();

        if (normalized.Count == 0)
        {
            throw new BusinessException(KnowledgeErrorCodes.NoTextExtracted);
        }

        return new ExtractedDocument(normalized);
    }
    protected abstract Task<List<ExtractedSection>> ExtractSectionsAsync(Stream content, CancellationToken cancellationToken);
    
    protected virtual string NormalizeText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        text = text.Normalize(NormalizationForm.FormC)
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Replace(' ', ' ');

        var builder = new StringBuilder(text.Length);
        foreach (var c in text)
        {
            if (c is '\n' or '\t' || !char.IsControl(c))
            {
                builder.Append(c);
            }
        }

        text = TrailingSpacesRegex().Replace(builder.ToString(), string.Empty);
        text = ExtraBlankLinesRegex().Replace(text, "\n\n");

        return text.Trim();
    }

    protected virtual string NormalizeHeading(string heading)
    {
        return WhitespaceRegex().Replace(NormalizeText(heading), " ");
    }

    [GeneratedRegex(@"[ \t]+$", RegexOptions.Multiline)]
    private static partial Regex TrailingSpacesRegex();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex ExtraBlankLinesRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
