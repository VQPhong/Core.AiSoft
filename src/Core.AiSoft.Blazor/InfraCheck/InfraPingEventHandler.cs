using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Timing;

namespace Core.AiSoft.Blazor.InfraCheck;

public class InfraPingEventHandler : IDistributedEventHandler<InfraPingEto>, ITransientDependency
{
    private readonly InfraPingTracker _tracker;
    private readonly IClock _clock;
    private readonly ILogger<InfraPingEventHandler> _logger;

    public InfraPingEventHandler(InfraPingTracker tracker, IClock clock, ILogger<InfraPingEventHandler> logger)
    {
        _tracker = tracker;
        _clock = clock;
        _logger = logger;
    }

    public Task HandleEventAsync(InfraPingEto eventData)
    {
        _tracker.MarkReceived(eventData.PingId, _clock.Now);
        _logger.LogInformation("Received infra ping {PingId} sent at {SentAt}", eventData.PingId, eventData.SentAt);
        return Task.CompletedTask;
    }
}
