using System;
using Volo.Abp;

namespace Core.AiSoft.Messaging;

public enum JobDeliveryDecision
{
    /// <summary>Processed successfully.</summary>
    Ack,

    /// <summary>Business error.</summary>
    Fail,

    /// <summary>Transient error with attempts left.</summary>
    Retry,

    /// <summary>Transient error on the last attempt / unreadable -> move to dead-letter queue.</summary>
    Dead
}

public static class JobRetryPolicy
{
    public static JobDeliveryDecision Decide(Exception? error, int attempt, int maxAttempts)
    {
        return error switch
        {
            null => JobDeliveryDecision.Ack,
            IBusinessException => JobDeliveryDecision.Fail,
            _ when attempt < maxAttempts => JobDeliveryDecision.Retry,
            _ => JobDeliveryDecision.Dead
        };
    }
}
