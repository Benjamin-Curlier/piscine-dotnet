var builder = DistributedApplication.CreateBuilder(args);

// TODO : ajoute un conteneur NATS avec JetStream et un volume durable.
// TODO : ajoute l'API et le worker, référence NATS et attends sa disponibilité.

builder.Build().Run();
