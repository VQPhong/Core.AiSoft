using System;
using Core.AiSoft.Knowledge.IngestionJobs;
using Shouldly;
using Xunit;

namespace Core.AiSoft.Knowledge;

public class IngestionJob_Tests
{
    private static readonly DateTime Start = new(2026, 10, 25, 10, 0, 0, DateTimeKind.Local);

    [Fact]
    public void New_Job_Is_Queued()
    {
        var job = CreateJob();

        job.Status.ShouldBe(IngestionJobStatus.Queued);
        job.AttemptCount.ShouldBe(0);
        job.IsFinished.ShouldBeFalse();
    }

    [Fact]
    public void StartAttempt_Keeps_First_Start_Time()
    {
        var job = CreateJob();

        job.StartAttempt(1, Start);
        job.StartAttempt(2, Start.AddSeconds(30));

        job.Status.ShouldBe(IngestionJobStatus.Extracting);
        job.AttemptCount.ShouldBe(2);
        job.StartedAt.ShouldBe(Start);
    }

    [Fact]
    public void Succeed_Finishes_The_Job()
    {
        var job = CreateJob();
        job.StartAttempt(1, Start);

        job.Succeed(12, Start.AddSeconds(5));

        job.Status.ShouldBe(IngestionJobStatus.Succeeded);
        job.ChunkCount.ShouldBe(12);
        job.FinishedAt.ShouldBe(Start.AddSeconds(5));
        job.IsFinished.ShouldBeTrue();
    }

    [Fact]
    public void Fail_Truncates_Long_Error_Message()
    {
        var job = CreateJob();

        job.Fail("Knowledge:BlobNotFound", new string('x', IngestionJobConsts.MaxErrorMessageLength + 100), Start);

        job.Status.ShouldBe(IngestionJobStatus.Failed);
        job.ErrorCode.ShouldBe("Knowledge:BlobNotFound");
        job.ErrorMessage!.Length.ShouldBe(IngestionJobConsts.MaxErrorMessageLength);
    }

    [Fact]
    public void Finished_Job_Cannot_Change()
    {
        var job = CreateJob();
        job.Succeed(1, Start);

        Should.Throw<InvalidOperationException>(() => job.StartAttempt(2, Start));
        Should.Throw<InvalidOperationException>(() => job.Fail(null, "late failure", Start));
    }

    private static IngestionJob CreateJob()
    {
        return new IngestionJob(Guid.NewGuid(), tenantId: null, documentId: Guid.NewGuid());
    }
}
