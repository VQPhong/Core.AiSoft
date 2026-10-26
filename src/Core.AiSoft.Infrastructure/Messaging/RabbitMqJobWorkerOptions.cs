using System;

namespace Core.AiSoft.Messaging;

public class RabbitMqJobWorkerOptions
{
    /// <summary>
    /// Turn off to stop this host from consuming the job (messages stay queued),
    /// </summary>
    public bool Enabled { get; set; } = true;

    public int MaxConcurrency { get; set; } = 2;

    public int MaxAttempts { get; set; } = 3;

    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(30);
}
