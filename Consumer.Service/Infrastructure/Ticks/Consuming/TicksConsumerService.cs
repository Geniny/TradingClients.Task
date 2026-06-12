using Consumer.Service.Application.Metrics;
using Consumer.Service.Application.Ticks;
using Consumer.Service.Domain.Metrics;
using Consumer.Service.Domain.Ticks;
using Microsoft.Extensions.Options;
using Producer.Shared.Exchanges;

namespace Consumer.Service.Infrastructure.Ticks.Consuming;

public class TicksConsumerService(
    TicksConsumerWebSocketClient webSocketClient,
    TickProcessingPipeline pipeline,
    IMetricProvider metricProvider,
    IOptions<TicksConsumeOptions> options,
    ILogger<TicksConsumerService> logger) : BackgroundService
{
    private readonly TicksConsumeOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting consumer service.");

        var tasks = new List<Task>();

        foreach (var clientConfiguration in _options.Clients)
        {
            tasks.Add(ListenProducerAsync(
                clientConfiguration.ClientId,
                clientConfiguration.ExchangeType,
                ToWsUri(clientConfiguration.ProducerUrl),
                stoppingToken));
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(seconds: 1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            logger.LogInformation(
                "Ticks: Received={Received}, Normalized={Normalized}, Duplicates={Duplicates}, Writes={Persisted}, Errors={Errors}. \n" +
                "Connections: Errors={ConnectionErrors}.",
                metricProvider.GetValue(MetricType.TicksReceived),
                metricProvider.GetValue(MetricType.TicksNormalized),
                metricProvider.GetValue(MetricType.TickDuplicates),
                metricProvider.GetValue(MetricType.TickWrites),
                metricProvider.GetValue(MetricType.TickErrors),
                metricProvider.GetValue(MetricType.ConnectionErrors));
        }

        await Task.WhenAll(tasks);
    }

    private async Task ListenProducerAsync(
        string clientId,
        ExchangeType exchange,
        Uri endpoint,
        CancellationToken stoppingToken)
    {
        var connectTriesCount = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("Client {ClientNumber} connecting to {Endpoint}.", clientId, endpoint);

                await webSocketClient.ConsumeAsync(endpoint, async (payload, token) =>
                {
                    metricProvider.Increment(MetricType.TicksReceived);

                    try
                    {
                        await pipeline.ProcessAsync(exchange, payload, token);
                    }
                    catch (Exception ex)
                    {
                        metricProvider.Increment(MetricType.TickErrors);
                        logger.LogError(ex, "Client {ClientNumber} failed to process payload", clientId);
                    }
                }, stoppingToken);

                connectTriesCount = 0;

                logger.LogWarning("Client {ClientNumber} disconnected from {Endpoint}", clientId, endpoint);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception)
            {
                metricProvider.Increment(MetricType.ConnectionErrors);

                logger.LogError("Client {ClientNumber} failed", clientId);
            }

            connectTriesCount++;

            logger.LogInformation("Client {ClientNumber} reconnect in {DelayMs} ms. Attempts {AttemptsCount}",
                clientId,
                _options.ReconnectDelay,
                connectTriesCount);

            await Task.Delay(_options.ReconnectDelay, stoppingToken);
        }
    }

    private static Uri ToWsUri(string rawEndpoint)
    {
        var uri = new Uri(rawEndpoint, UriKind.Absolute);
        var builder = new UriBuilder(uri)
        {
            Scheme = uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ? "wss" : "ws",
            Port = uri.IsDefaultPort ? -1 : uri.Port,
        };

        if (string.IsNullOrWhiteSpace(builder.Path) || builder.Path == "/")
        {
            builder.Path = "/ws";
        }

        return builder.Uri;
    }
}