var builder = DistributedApplication.CreateBuilder(args);

var nats = builder.AddContainer("nats", "nats", "2.11-alpine")
    .WithArgs("-js")
    .WithDataVolume();

builder.AddProject<Projects.Api>("api")
    .WithReference(nats)
    .WaitFor(nats)
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.Worker>("worker")
    .WithReference(nats)
    .WaitFor(nats);

builder.Build().Run();
