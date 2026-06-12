using Microsoft.Extensions.Options;
using Producer.Service.Application.Ticks;
using Producer.Service.Domain;
using Producer.Service.Infrastructure.Ticks.Streaming;

namespace Producer.Service.Infrastructure.Ticks.Producing;

public sealed class TicksProducerService(
    ITickPayloadFactory tickPayloadFactory,
    TicksBroadcastHub hub,
    IOptions<ProducerOptions> options,
    ILogger<TicksProducerService> logger) : BackgroundService
{
    private readonly ProducerOptions _options = options.Value;
    private long _sent;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var tickDelay = TimeSpan.FromMilliseconds(1000d / _options.TicksRatePerSecond);

        logger.LogInformation(
            "Starting producer service. Exchange={ExchangeType}, Rate={Rate} ticks/sec.",
            _options.ExchangeType,
            _options.TicksRatePerSecond);

        using var telemetryTimer = new PeriodicTimer(TimeSpan.FromSeconds(seconds: 5));

        var telemetryTask = Task.Run(async () =>
        {
            while (await telemetryTimer.WaitForNextTickAsync(stoppingToken))
            {
                logger.LogInformation(
                    "Producer stats: exchange={ExchangeType}, sent={Sent}, clients={ConnectedClients}.",
                    _options.ExchangeType,
                    _sent,
                    hub.ConnectedClientsCount);
            }
        }, stoppingToken);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var currentTick = Interlocked.Read(ref _sent);
                var shouldCreateDuplicateTick = currentTick > 0 && currentTick % _options.DuplicateEveryNTick == 0;
                var payload = tickPayloadFactory.Create(options.Value.ExchangeType, shouldCreateDuplicateTick);

                await hub.BroadcastAsync(payload, stoppingToken);

                Interlocked.Increment(ref _sent);

                await Task.Delay(tickDelay, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // expected during shutdown
        }
        finally
        {
            telemetryTimer.Dispose();
            try
            {
                await telemetryTask;
            }
            catch (OperationCanceledException)
            {
                // expected on shutdown
            }
        }
    }
}