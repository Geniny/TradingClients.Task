using Consumer.Service.Domain.Ticks;

namespace Consumer.Service.Application.Ticks.Storage;

public class TickQueueWriter(TickWriteQueue tickWriteQueue) : ITickWriter
{
    public ValueTask WriteAsync(TickData tickData, CancellationToken cancellationToken = default)
    {
        return tickWriteQueue.EnqueueAsync(tickData, cancellationToken);
    }
}