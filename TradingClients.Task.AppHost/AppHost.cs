using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres", port: 60288)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume();

var ticksDb = postgres.AddDatabase("ticks-db");

var cryptoProducer = builder.AddProject<Producer_Service>("crypto-producer")
    .WithHttpEndpoint(port: 8081)
    .WithEnvironment("Producer__ExchangeType", "Crypto")
    .WithEnvironment("Producer__TickRatePerSecond", "45");

var futuresProducer = builder.AddProject<Producer_Service>("futures-producer")
    .WithHttpEndpoint(port: 8082)
    .WithEnvironment("Producer__ExchangeType", "Futures")
    .WithEnvironment("Producer__TickRatePerSecond", "45");

builder.AddProject<Consumer_Service>("consumer-service")
    .WithReference(ticksDb, "Database")
    .WithReference(cryptoProducer)
    .WithReference(futuresProducer)
    .WaitFor(ticksDb)
    .WaitFor(cryptoProducer)
    .WaitFor(futuresProducer)
    .WithEnvironment("TicksConsume__Clients__0__ClientId", "futures-client")
    .WithEnvironment("TicksConsume__Clients__0__ProducerUrl", $"{futuresProducer.GetEndpoint("http")}/ws")
    .WithEnvironment("TicksConsume__Clients__0__ExchangeType", "Futures")
    .WithEnvironment("TicksConsume__Clients__1__ClientId", "crypto-client")
    .WithEnvironment("TicksConsume__Clients__1__ProducerUrl", $"{cryptoProducer.GetEndpoint("http")}/ws")
    .WithEnvironment("TicksConsume__Clients__1__ExchangeType", "Crypto");

builder.Build().Run();