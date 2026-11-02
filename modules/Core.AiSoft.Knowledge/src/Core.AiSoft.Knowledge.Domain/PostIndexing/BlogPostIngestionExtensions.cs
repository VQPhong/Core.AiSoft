using System;
using System.Security.Cryptography;
using System.Text;
using Volo.Abp;
using Volo.CmsKit.Blogs;

namespace Core.AiSoft.Knowledge.PostIndexing;

public static class BlogPostIngestionExtensions
{
    public static void EnsureIngestible(this BlogPost post, Guid knowledgeBlogId)
    {
        EnsureIngestible(post.BlogId, post.Status, knowledgeBlogId);
    }

    public static string ComputeContentHash(this BlogPost post)
    {
        return ComputeContentHash(post.Content);
    }

    internal static void EnsureIngestible(Guid blogId, BlogPostStatus status, Guid knowledgeBlogId)
    {
        if (blogId != knowledgeBlogId)
        {
            throw new BusinessException(KnowledgeErrorCodes.PostNotInKnowledgeBlog);
        }

        if (status != BlogPostStatus.Published)
        {
            throw new BusinessException(KnowledgeErrorCodes.PostNotPublished);
        }
    }

    internal static string ComputeContentHash(string? content)
    {
        var normalized = (content ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }
}
