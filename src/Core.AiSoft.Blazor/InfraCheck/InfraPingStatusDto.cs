using System;

namespace Core.AiSoft.Blazor.InfraCheck;

public class InfraPingStatusDto
{
    public Guid PingId { get; set; }

    public bool Received { get; set; }

    public DateTime? ReceivedAt { get; set; }
}
