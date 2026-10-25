using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Domain.Services;

namespace Core.AiSoft.Knowledge.Documents;

public class KnowledgeFileValidator : DomainService
{
    private static readonly byte[] PdfSignature = "%PDF-"u8.ToArray();
    private static readonly byte[] ZipSignature = [0x50, 0x4B, 0x03, 0x04];
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    private readonly KnowledgeUploadOptions _options;

    public KnowledgeFileValidator(IOptions<KnowledgeUploadOptions> options)
    {
        _options = options.Value;
    }

    /// <returns>Normalized extension and content type stored.</returns>
    public virtual (string Extension, string ContentType) Validate(string fileName, byte[] content)
    {
        Check.NotNullOrWhiteSpace(fileName, nameof(fileName));
        Check.NotNull(content, nameof(content));

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!_options.AllowedExtensions.TryGetValue(extension, out var contentType))
        {
            throw new BusinessException(KnowledgeErrorCodes.FileTypeNotAllowed)
                .WithData("extension", string.IsNullOrEmpty(extension) ? fileName : extension)
                .WithData("allowedExtensions", string.Join(", ", _options.AllowedExtensions.Keys));
        }

        EnsureSize(content.LongLength);

        if (!HasValidContent(extension, content))
        {
            throw new BusinessException(KnowledgeErrorCodes.FileContentMismatch)
                .WithData("fileName", fileName)
                .WithData("extension", extension);
        }

        return (extension, contentType);
    }

    public virtual void EnsureSize(long size)
    {
        if (size == 0)
        {
            throw new BusinessException(KnowledgeErrorCodes.EmptyFile);
        }

        if (size > _options.MaxFileSize)
        {
            throw new BusinessException(KnowledgeErrorCodes.FileTooLarge)
                .WithData("maxSizeMb", _options.MaxFileSize / (1024 * 1024));
        }
    }

    protected virtual bool HasValidContent(string extension, byte[] content)
    {
        return extension switch
        {
            ".pdf" => StartsWith(content, PdfSignature),
            ".docx" => IsWordDocument(content),
            ".md" or ".txt" => IsUtf8Text(content),
            _ => false
        };
    }

    private static bool StartsWith(byte[] content, byte[] signature)
    {
        return content.Length >= signature.Length && content.AsSpan(0, signature.Length).SequenceEqual(signature);
    }

    private static bool IsWordDocument(byte[] content)
    {
        if (!StartsWith(content, ZipSignature))
        {
            return false;
        }

        try
        {
            using var archive = new ZipArchive(new MemoryStream(content, writable: false), ZipArchiveMode.Read);
            return archive.Entries.Any(e => e.FullName == "word/document.xml");
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }

    private static bool IsUtf8Text(byte[] content)
    {
        if (Array.IndexOf(content, (byte)0) >= 0)
        {
            return false;
        }

        try
        {
            StrictUtf8.GetCharCount(content);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }
}
