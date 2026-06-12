using Consumer.Service.Domain.Ticks;

namespace Consumer.Service.Application.Ticks.Storage;

public interface ITickRepository
{
    Task EnsureSchemaAsync(CancellationToken cancellationToken);
    Task<int> InsertBatchAsync(IReadOnlyCollection<TickData> ticks, CancellationToken cancellationToken);
}