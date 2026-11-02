using System;
using System.Net.Http;
using Core.AiSoft.Messaging;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace Core.AiSoft.Messaging;

public class JobRetryPolicy_Tests
{
    [Fact]
    public void Success_Is_Acknowledged()
    {
        JobRetryPolicy.Decide(null, attempt: 1, maxAttempts: 3).ShouldBe(JobDeliveryDecision.Ack);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void Business_Error_Is_Never_Retried(int attempt)
    {
        JobRetryPolicy.Decide(new BusinessException("Knowledge:BlobNotFound"), attempt, maxAttempts: 3)
            .ShouldBe(JobDeliveryDecision.Fail);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Transient_Error_With_Attempts_Left_Is_Retried(int attempt)
    {
        JobRetryPolicy.Decide(new HttpRequestException("connection refused"), attempt, maxAttempts: 3)
            .ShouldBe(JobDeliveryDecision.Retry);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    public void Transient_Error_On_Last_Attempt_Goes_Dead(int attempt)
    {
        JobRetryPolicy.Decide(new TimeoutException(), attempt, maxAttempts: 3)
            .ShouldBe(JobDeliveryDecision.Dead);
    }
}
