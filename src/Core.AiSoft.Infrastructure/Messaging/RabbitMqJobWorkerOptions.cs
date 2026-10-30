using System;

namespace Core.AiSoft.Messaging;

public class RabbitMqJobWorkerOptions
{
    /// <summary>
    /// Turn off to stop this host from consuming the job (messages stay queued),
    /// </summary>
    public bool Enabled { get; set; } = true;

    public const int MaxConcurrencyLimit = 16;

    /// <summary>
    /// Messages processed in parallel by this host (clamped to 1..<see cref="MaxConcurrencyLimit"/>). The total across hosts
    /// is hosts x this value.
    /// </summary>
    public int MaxConcurrency { get; set; } = 2;

    public int MaxAttempts { get; set; } = 3;

    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(30);
}
