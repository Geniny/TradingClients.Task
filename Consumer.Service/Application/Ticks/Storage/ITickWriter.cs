using Consumer.Service.Domain.Ticks;

namespace Consumer.Service.Application.Ticks.Storage;

public interface ITickWriter
{
    ValueTask WriteAsync(TickData tickData, CancellationToken cancellationToken = default);
}