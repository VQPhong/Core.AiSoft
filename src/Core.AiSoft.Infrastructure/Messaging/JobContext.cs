using System;

namespace Core.AiSoft.Messaging;

/// <param name="Attempt">1-based delivery attempt.</param>
public record JobContext(int Attempt, int MaxAttempts, string? MessageId)
{
    public bool IsLastAttempt => Attempt >= MaxAttempts;
}

/// <param name="RetriesExhausted">True when failed with a transient error on last attempt; false for a business error.</param>
public record JobFailure(Exception Exception, string? ErrorCode, string? ErrorMessage, int Attempt, bool RetriesExhausted);
