var builder = DistributedApplication.CreateBuilder(args);

// TODO : NATS JetStream durable.
// TODO : API et worker référencent et attendent le broker.
// TODO : health check HTTP de l'API.

builder.Build().Run();
