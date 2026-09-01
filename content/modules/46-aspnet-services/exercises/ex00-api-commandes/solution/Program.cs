var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<ICommandBus, InMemoryCommandBus>();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new { status = "ready" }));
app.MapPost("/commands", async (SubmitCommand command, ICommandBus bus, CancellationToken cancellationToken) =>
{
    var id = await bus.EnqueueAsync(command, cancellationToken);
    return Results.Accepted($"/commands/{id}", new { id });
});

app.Run();

public sealed record SubmitCommand(string Kind, string Payload);

public interface ICommandBus
{
    Task<Guid> EnqueueAsync(SubmitCommand command, CancellationToken cancellationToken);
}

public sealed class InMemoryCommandBus : ICommandBus
{
    public Task<Guid> EnqueueAsync(SubmitCommand command, CancellationToken cancellationToken) =>
        Task.FromResult(Guid.NewGuid());
}
