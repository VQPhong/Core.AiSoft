using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Core.AiSoft.Knowledge.PostIndexing;
using Grpc.Core;
using Microsoft.Extensions.AI;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using Volo.Abp.DependencyInjection;
using static Qdrant.Client.Grpc.Conditions;

namespace Core.AiSoft.Knowledge;

public class QdrantKnowledgeVectorStore : IKnowledgeVectorStore, ITransientDependency
{
    public const string CollectionPrefix = "knowledge_";

    public const string BlogPostIdField = "blog_post_id";
    public const string TenantIdField = "tenant_id";
    public const string VersionField = "version";
    public const string ChunkIndexField = "chunk_index";
    public const string HeadingPathField = "heading_path";
    public const string TextField = "text";
    public const string PageNumberField = "page_number";

    private const int EmbeddingBatchSize = 16;

    private readonly QdrantClient _qdrantClient;
    private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;

    public QdrantKnowledgeVectorStore(QdrantClient qdrantClient, IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
    {
        _qdrantClient = qdrantClient;
        _embeddingGenerator = embeddingGenerator;
    }

    public virtual async Task ReplaceBlogPostAsync(KnowledgePostChunks post, CancellationToken cancellationToken = default)
    {
        if (post.Chunks.Count == 0)
        {
            await DeleteByBlogPostAsync(post.BlogPostId, cancellationToken);
            return;
        }

        var collection = GetCollectionName();
        var points = new List<PointStruct>(post.Chunks.Count);

        foreach (var batch in post.Chunks.Chunk(EmbeddingBatchSize))
        {
            var embeddings = await _embeddingGenerator.GenerateAsync(batch.Select(ToEmbeddingInput), cancellationToken: cancellationToken);

            if (points.Count == 0)
            {
                await EnsureCollectionAsync(collection, embeddings[0].Vector.Length, cancellationToken);
            }

            points.AddRange(batch.Select((chunk, i) => ToPoint(post, chunk, embeddings[i].Vector.ToArray())));
        }

        await _qdrantClient.UpsertAsync(collection, points, wait: true, cancellationToken: cancellationToken);

        await _qdrantClient.DeleteAsync(
            collection,
            new Filter
            {
                Must = { MatchKeyword(BlogPostIdField, post.BlogPostId.ToString()) },
                MustNot = { HasId(points.Select(p => Guid.Parse(p.Id.Uuid)).ToList()) }
            },
            wait: true,
            cancellationToken: cancellationToken);
    }

    public virtual async Task DeleteByBlogPostAsync(Guid blogPostId, CancellationToken cancellationToken = default)
    {
        var collections = await _qdrantClient.ListCollectionsAsync(cancellationToken);

        foreach (var collection in collections.Where(c => c.StartsWith(CollectionPrefix, StringComparison.Ordinal)))
        {
            await _qdrantClient.DeleteAsync(
                collection,
                MatchKeyword(BlogPostIdField, blogPostId.ToString()),
                wait: true,
                cancellationToken: cancellationToken);
        }
    }

    protected virtual string GetCollectionName()
    {
        var model = _embeddingGenerator.GetService<EmbeddingGeneratorMetadata>()?.DefaultModelId
                    ?? throw new InvalidOperationException("The embedding generator does not expose its model name.");

        return CollectionPrefix + Regex.Replace(model.ToLowerInvariant(), "[^a-z0-9]+", "_").Trim('_');
    }

    protected virtual async Task EnsureCollectionAsync(string collection, int dimensions, CancellationToken cancellationToken)
    {
        if (await _qdrantClient.CollectionExistsAsync(collection, cancellationToken))
        {
            return;
        }

        try
        {
            await _qdrantClient.CreateCollectionAsync(
                collection,
                new VectorParams { Size = (ulong)dimensions, Distance = Distance.Cosine },
                cancellationToken: cancellationToken);
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.AlreadyExists)
        {
            return;
        }

        await _qdrantClient.CreatePayloadIndexAsync(collection, BlogPostIdField, PayloadSchemaType.Keyword, cancellationToken: cancellationToken);
        await _qdrantClient.CreatePayloadIndexAsync(collection, TenantIdField, PayloadSchemaType.Keyword, cancellationToken: cancellationToken);
    }

    protected virtual string ToEmbeddingInput(KnowledgeChunk chunk)
        => chunk.HeadingPath.Count == 0 ? chunk.Text : $"{string.Join(" > ", chunk.HeadingPath)}\n\n{chunk.Text}";

    protected virtual PointStruct ToPoint(KnowledgePostChunks post, KnowledgeChunk chunk, float[] vector)
    {
        var point = new PointStruct
        {
            Id = PointId(post.BlogPostId, chunk.Index),
            Vectors = vector,
            Payload =
            {
                [BlogPostIdField] = post.BlogPostId.ToString(),
                [VersionField] = post.Version,
                [ChunkIndexField] = chunk.Index,
                [HeadingPathField] = chunk.HeadingPath.ToArray(),
                [TextField] = chunk.Text
            }
        };

        if (post.TenantId.HasValue)
        {
            point.Payload[TenantIdField] = post.TenantId.Value.ToString();
        }

        if (chunk.PageNumber.HasValue)
        {
            point.Payload[PageNumberField] = chunk.PageNumber.Value;
        }

        return point;
    }

    public static Guid PointId(Guid blogPostId, int chunkIndex)
        => new(MD5.HashData(Encoding.UTF8.GetBytes($"{blogPostId:N}:{chunkIndex}")));
}
