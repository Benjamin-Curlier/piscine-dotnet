using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
builder.Services.AddAuthorization(options =>
    options.AddPolicy("send-command", policy => policy.RequireClaim("scope", "asteria.command.send")));
builder.Services.AddScoped<ICommandApplication, CommandApplication>();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/commands", async (
    HttpRequest request,
    SubmitCommand command,
    ICommandApplication application,
    CancellationToken cancellationToken) =>
{
    if (!request.Headers.TryGetValue("Idempotency-Key", out var key) || string.IsNullOrWhiteSpace(key))
    {
        return Results.Problem("Idempotency-Key required", statusCode: 400);
    }

    var id = await application.SubmitAsync(key!, command, cancellationToken);
    return Results.Accepted($"/commands/{id}", new { id });
}).RequireAuthorization("send-command");

app.Run();

public sealed record SubmitCommand(string Kind, string Payload);
public interface ICommandApplication
{
    Task<Guid> SubmitAsync(string key, SubmitCommand command, CancellationToken cancellationToken);
}
public sealed class CommandApplication : ICommandApplication
{
    public Task<Guid> SubmitAsync(string key, SubmitCommand command, CancellationToken cancellationToken) =>
        Task.FromResult(Guid.NewGuid());
}

public partial class Program
{
}
