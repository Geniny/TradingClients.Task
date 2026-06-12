using Consumer.Service.Application.Metrics;
using Consumer.Service.Application.Ticks.Storage;
using Consumer.Service.Domain.Metrics;
using Consumer.Service.Domain.Ticks;
using Microsoft.Extensions.Options;

namespace Consumer.Service.Infrastructure.Ticks.Writing;

public sealed class TicksWriterService(
    TimeProvider timeProvider,
    TickWriteQueue writeQueue,
    ITickRepository tickRepository,
    IMetricProvider metricProvider,
    IOptions<TicksWriteOptions> options,
    ILogger<TicksWriterService> logger) : BackgroundService
{
    private readonly TicksWriteOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting writer service");

        await tickRepository.EnsureSchemaAsync(stoppingToken);

        var ticksBatchList = new List<TickData>(_options.WriteSize);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ReadBatchAsync(ticksBatchList, _options.WriteSize, _options.WriteTimeout, stoppingToken);

                await WriteAsync(ticksBatchList, stoppingToken);

                ticksBatchList.Clear();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // expected on shutdown
        }
        finally
        {
            await WriteRemainingAsync(ticksBatchList, stoppingToken);
        }
    }

    private async Task ReadBatchAsync(
        List<TickData> ticksBatchList,
        int writeSize,
        TimeSpan writeTimeout,
        CancellationToken stoppingToken)
    {
        ticksBatchList.Clear();
        ticksBatchList.Add(await writeQueue.Reader.ReadAsync(stoppingToken));

        var writeAt = timeProvider.GetUtcNow() + writeTimeout;

        while (ticksBatchList.Count < writeSize)
        {
            if (writeQueue.Reader.TryRead(out var tickData))
            {
                ticksBatchList.Add(tickData);

                continue;
            }

            if (!await WaitForNextTickBeforeAsync(writeAt, stoppingToken))
            {
                return;
            }
        }
    }

    private async Task<bool> WaitForNextTickBeforeAsync(DateTimeOffset writeAt, CancellationToken stoppingToken)
    {
        var timeout = writeAt - timeProvider.GetUtcNow();
        if (timeout <= TimeSpan.Zero)
        {
            return false;
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

        timeoutCts.CancelAfter(timeout);

        try
        {
            return await writeQueue.Reader.WaitToReadAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
        {
            return false;
        }
    }

    private async Task WriteRemainingAsync(List<TickData> ticksBatchList, CancellationToken cancellationToken)
    {
        while (writeQueue.Reader.TryRead(out var tickData))
        {
            ticksBatchList.Add(tickData);

            if (ticksBatchList.Count >= _options.WriteSize)
            {
                await WriteAsync(ticksBatchList, cancellationToken);

                ticksBatchList.Clear();
            }
        }

        if (ticksBatchList.Count > 0)
        {
            await WriteAsync(ticksBatchList, cancellationToken);
        }
    }

    private async Task WriteAsync(IReadOnlyCollection<TickData> ticksBatchList, CancellationToken stoppingToken)
    {
        var insertedTicksCount = await tickRepository.InsertBatchAsync(ticksBatchList, stoppingToken);
        metricProvider.Increment(MetricType.TickWrites, insertedTicksCount);
    }
}