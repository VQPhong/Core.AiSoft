using System;
using Volo.Abp.EventBus;

namespace Core.AiSoft.Blazor.InfraCheck;

[EventName("Core.AiSoft.InfraPing")]
public class InfraPingEto
{
    public Guid PingId { get; set; }

    public DateTime SentAt { get; set; }
}
