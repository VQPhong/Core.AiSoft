using System;
using Core.AiSoft.Knowledge.IngestionJobs;
using Core.AiSoft.Knowledge.PostIndexing;
using Shouldly;
using Volo.Abp;
using Volo.CmsKit.Blogs;
using Xunit;

namespace Core.AiSoft.Knowledge;

public class KnowledgePostIndexState_Tests
{
    private static readonly DateTime Now = new(2026, 10, 29, 9, 0, 0, DateTimeKind.Local);
    private static readonly string HashA = new('a', 64);
    private static readonly string HashB = new('b', 64);

    private readonly Guid _postId = Guid.NewGuid();

    [Fact]
    public void New_Post_Can_Be_Enqueued()
    {
        var state = NewState();

        state.EnsureCanEnqueue(HashA, force: false);
        state.GetStatus(HashA).ShouldBe(PostIndexStatus.NeverIndexed);
    }

    [Fact]
    public void Active_Job_Blocks_A_Second_Job_Even_When_Forced()
    {
        var state = NewState();
        var job = Job(version: 1, HashA);
        state.BeginJob(job.Id, job.Version);

        ShouldFailWith(KnowledgeErrorCodes.IngestionAlreadyRunning, () => state.EnsureCanEnqueue(HashB, force: true));
        ShouldFailWith(KnowledgeErrorCodes.IngestionAlreadyRunning, () => state.BeginJob(Guid.NewGuid(), 2));
        state.GetStatus(HashB).ShouldBe(PostIndexStatus.Running);
    }

    [Fact]
    public void Unchanged_Content_Is_Rejected_Unless_Forced()
    {
        var state = IndexedState(version: 1, HashA);

        ShouldFailWith(KnowledgeErrorCodes.ContentUnchanged, () => state.EnsureCanEnqueue(HashA, force: false));
        state.EnsureCanEnqueue(HashA, force: true);
        state.EnsureCanEnqueue(HashB, force: false);
    }

    [Fact]
    public void Status_Follows_The_Current_Content()
    {
        var state = IndexedState(version: 1, HashA);

        state.GetStatus(HashA).ShouldBe(PostIndexStatus.UpToDate);
        state.GetStatus(HashB).ShouldBe(PostIndexStatus.Stale);
    }

    [Fact]
    public void Job_That_Is_Not_The_Active_One_Is_Superseded()
    {
        var state = NewState();
        var active = Job(version: 2, HashB);
        state.BeginJob(active.Id, active.Version);

        state.ShouldSkip(Job(version: 1, HashA)).ShouldBe(IngestionJobSkipReason.Superseded);
        state.ShouldSkip(active).ShouldBeNull();
    }

    [Fact]
    public void Older_Version_Than_The_Indexed_One_Is_Superseded()
    {
        var state = IndexedState(version: 5, HashA);
        var old = Job(version: 3, HashB);
        state.BeginJob(old.Id, old.Version);

        state.ShouldSkip(old).ShouldBe(IngestionJobSkipReason.Superseded);
    }

    [Fact]
    public void Already_Indexed_Content_Is_Skipped_Unless_Forced()
    {
        var state = IndexedState(version: 1, HashA);

        var same = Job(version: 2, HashA);
        state.BeginJob(same.Id, same.Version);
        state.ShouldSkip(same).ShouldBe(IngestionJobSkipReason.Unchanged);
        state.EndJob(same.Id);

        var forced = Job(version: 2, HashA, isForced: true);
        state.BeginJob(forced.Id, forced.Version);
        state.ShouldSkip(forced).ShouldBeNull();
    }

    [Fact]
    public void Result_Of_An_Older_Version_Does_Not_Overwrite_A_Newer_One()
    {
        var state = IndexedState(version: 5, HashB);

        state.ApplyResult(Job(version: 3, HashA), chunkCount: 99, Now).ShouldBeFalse();

        state.IndexedVersion.ShouldBe(5);
        state.IndexedContentHash.ShouldBe(HashB);
    }

    [Fact]
    public void EndJob_Only_Releases_The_Slot_Held_By_That_Job()
    {
        var state = NewState();
        var job = Job(version: 1, HashA);
        state.BeginJob(job.Id, job.Version);

        state.EndJob(Guid.NewGuid());
        state.HasActiveJob.ShouldBeTrue();

        state.EndJob(job.Id);
        state.HasActiveJob.ShouldBeFalse();
        state.LastJobId.ShouldBe(job.Id);
    }

    [Fact]
    public void Post_With_An_Active_Job_Cannot_Be_Deleted()
    {
        var state = IndexedState(version: 1, HashA);
        state.EnsureCanDelete();

        var job = Job(version: 2, HashB);
        state.BeginJob(job.Id, job.Version);
        ShouldFailWith(KnowledgeErrorCodes.IngestionAlreadyRunning, state.EnsureCanDelete);

        state.EndJob(job.Id);
        state.EnsureCanDelete();
    }

    [Fact]
    public void Only_Published_Posts_Of_The_Knowledge_Blog_Are_Ingestible()
    {
        var knowledgeBlogId = Guid.NewGuid();

        BlogPostIngestionExtensions.EnsureIngestible(knowledgeBlogId, BlogPostStatus.Published, knowledgeBlogId);
        ShouldFailWith(KnowledgeErrorCodes.PostNotPublished,
            () => BlogPostIngestionExtensions.EnsureIngestible(knowledgeBlogId, BlogPostStatus.Draft, knowledgeBlogId));
        ShouldFailWith(KnowledgeErrorCodes.PostNotInKnowledgeBlog,
            () => BlogPostIngestionExtensions.EnsureIngestible(Guid.NewGuid(), BlogPostStatus.Published, knowledgeBlogId));
    }

    [Fact]
    public void Content_Hash_Ignores_Line_Ending_Style_Only()
    {
        var lf = BlogPostIngestionExtensions.ComputeContentHash("# Title\nBody\n");

        BlogPostIngestionExtensions.ComputeContentHash("# Title\r\nBody\r\n").ShouldBe(lf);
        BlogPostIngestionExtensions.ComputeContentHash("# Title\nBody changed\n").ShouldNotBe(lf);
        lf.Length.ShouldBe(IngestionJobConsts.ContentHashLength);
    }

    private KnowledgePostIndexState NewState() => new(Guid.NewGuid(), tenantId: null, _postId);

    private KnowledgePostIndexState IndexedState(int version, string hash)
    {
        var state = NewState();
        var job = Job(version, hash);
        state.BeginJob(job.Id, job.Version);
        state.ApplyResult(job, chunkCount: 3, Now).ShouldBeTrue();
        state.EndJob(job.Id);
        return state;
    }

    private IngestionJob Job(int version, string hash, bool isForced = false)
        => IngestionJob_Tests.CreateJob(_postId, version, hash, isForced);

    private static void ShouldFailWith(string code, Action action)
        => Should.Throw<BusinessException>(action).Code.ShouldBe(code);
}
