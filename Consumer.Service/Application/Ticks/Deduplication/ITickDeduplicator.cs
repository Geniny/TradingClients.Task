using Consumer.Service.Domain.Ticks;

namespace Consumer.Service.Application.Ticks.Deduplication;

public interface ITickDeduplicator
{
    bool IsDuplicate(TickData tickData);
}