using Consumer.Service.Application.Metrics;
using Consumer.Service.Application.Ticks;
using Consumer.Service.Application.Ticks.Deduplication;
using Consumer.Service.Application.Ticks.Normalization;
using Consumer.Service.Application.Ticks.Normalization.Handlers;
using Consumer.Service.Application.Ticks.Storage;
using Consumer.Service.Domain.Ticks;
using Consumer.Service.Infrastructure.Persistence.Ticks;
using Consumer.Service.Infrastructure.Ticks.Consuming;
using Consumer.Service.Infrastructure.Ticks.Writing;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.Configure<TicksConsumeOptions>(builder.Configuration.GetSection("TicksConsume"));
builder.Services.Configure<TicksWriteOptions>(builder.Configuration.GetSection("TicksWrite"));

//Metrics
builder.Services.AddSingleton<IMetricProvider, MetricProvider>();

//Ticks Deduplication
builder.Services.AddSingleton<ITickDeduplicator, TickDeduplicatorWithExpiration>();

//Ticks Normalization
builder.Services.AddSingleton<ITickNormalizeHandler, CryptoTickNormalizeHandler>();
builder.Services.AddSingleton<ITickNormalizeHandler, FuturesTickNormalizeHandler>();
builder.Services.AddSingleton<ITickNormalizer, TickNormalizer>();

//Ticks
builder.Services.AddSingleton<ITickRepository, TickRepository>();
builder.Services.AddSingleton<ITickWriter, TickQueueWriter>();
builder.Services.AddSingleton<TickWriteQueue>();
builder.Services.AddSingleton<TicksConsumerWebSocketClient>();
builder.Services.AddSingleton<TickProcessingPipeline>();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddHostedService<TicksWriterService>();
builder.Services.AddHostedService<TicksConsumerService>();

await builder.Build().RunAsync();