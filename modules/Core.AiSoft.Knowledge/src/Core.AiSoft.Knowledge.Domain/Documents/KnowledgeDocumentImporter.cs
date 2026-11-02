using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.AiSoft.Knowledge.Extraction;
using Volo.Abp;
using Volo.Abp.Domain.Services;
using Volo.Abp.Users;
using Volo.CmsKit.Blogs;
using Volo.CmsKit.Users;

namespace Core.AiSoft.Knowledge.Documents;

/// <summary>
/// Upload pipeline: validate → extract text → render Markdown → store the source file → create a Draft post in the
/// Knowledge blog. Text is extracted before anything is stored, so an unreadable file (e.g. a scanned PDF) leaves nothing behind;
/// everything else is saved in the caller's unit of work. Ingestion is not started here.
/// </summary>
public class KnowledgeDocumentImporter : DomainService
{
    private readonly KnowledgeFileValidator _fileValidator;
    private readonly KnowledgeDocumentManager _documentManager;
    private readonly IEnumerable<ITextExtractor> _extractors;
    private readonly IBlogRepository _blogRepository;
    private readonly IBlogPostRepository _blogPostRepository;
    private readonly BlogPostManager _blogPostManager;
    private readonly ICmsUserLookupService _cmsUserLookupService;
    private readonly ICurrentUser _currentUser;

    public KnowledgeDocumentImporter(
        KnowledgeFileValidator fileValidator,
        KnowledgeDocumentManager documentManager,
        IEnumerable<ITextExtractor> extractors,
        IBlogRepository blogRepository,
        IBlogPostRepository blogPostRepository,
        BlogPostManager blogPostManager,
        ICmsUserLookupService cmsUserLookupService,
        ICurrentUser currentUser)
    {
        _fileValidator = fileValidator;
        _documentManager = documentManager;
        _extractors = extractors;
        _blogRepository = blogRepository;
        _blogPostRepository = blogPostRepository;
        _blogPostManager = blogPostManager;
        _cmsUserLookupService = cmsUserLookupService;
        _currentUser = currentUser;
    }

    public virtual async Task<(KnowledgeDocument Document, BlogPost Post)> ImportAsync(string fileName, byte[] content)
    {
        var (extension, _) = _fileValidator.Validate(fileName, content);
        var markdown = await ConvertToMarkdownAsync(extension, content);

        var document = await _documentManager.CreateAsync(fileName, content);

        var blog = await _blogRepository.GetBySlugAsync(KnowledgeBlogConsts.BlogSlug);
        var author = await _cmsUserLookupService.GetByIdAsync(_currentUser.GetId());
        var title = Path.GetFileNameWithoutExtension(fileName);

        var post = await _blogPostManager.CreateAsync(
            author,
            blog,
            Truncate(title, BlogPostConsts.MaxTitleLength),
            CreateSlug(title, document.Id),
            BlogPostStatus.Draft,
            Truncate($"Imported from {fileName}", BlogPostConsts.MaxShortDescriptionLength),
            markdown);

        await _blogPostRepository.InsertAsync(post);
        document.AttachPost(post.Id);

        return (document, post);
    }

    protected virtual async Task<string> ConvertToMarkdownAsync(string extension, byte[] content)
    {
        var extractor = _extractors.FirstOrDefault(e => e.CanExtract(extension))
                        ?? throw new BusinessException(KnowledgeErrorCodes.ExtractorNotFound).WithData("extension", extension);

        using var stream = new MemoryStream(content, writable: false);
        var extracted = await extractor.ExtractAsync(stream);

        return ExtractedDocumentMarkdownRenderer.Render(extracted);
    }

    /// <summary>
    /// ASCII slug from the file name (Vietnamese diacritics removed) plus a short id suffix so it is unique in the blog.
    /// </summary>
    protected virtual string CreateSlug(string title, Guid documentId)
    {
        var normalized = title.Replace('đ', 'd').Replace('Đ', 'D').Normalize(NormalizationForm.FormD);
        var slug = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsAsciiLetterOrDigit(c))
            {
                slug.Append(char.ToLowerInvariant(c));
            }
            else if (slug.Length > 0 && slug[^1] != '-')
            {
                slug.Append('-');
            }
        }

        var baseSlug = slug.ToString().Trim('-');
        var suffix = documentId.ToString("N")[..8];
        var maxBase = BlogPostConsts.MaxSlugLength - suffix.Length - 1;

        return baseSlug.Length == 0 ? $"document-{suffix}" : $"{Truncate(baseSlug, maxBase).TrimEnd('-')}-{suffix}";
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength];
}
