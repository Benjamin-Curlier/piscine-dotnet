global using System;
global using System.Collections.Generic;
global using System.ComponentModel.DataAnnotations;
global using System.Linq;
global using System.Net.Http;
global using System.Threading;
global using System.Threading.Tasks;
global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Http;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;

using System.Net;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

public sealed class R7ExecutableTests
{
    [Fact]
    public async Task Desktop_keeps_the_latest_result_and_cancels_the_previous_request()
    {
        var gateway = new ControlledGateway();
        var viewModel = new DesktopViewModel(gateway, new ImmediateDispatcher());

        var oldRequest = viewModel.SubmitAsync("ancienne", CancellationToken.None);
        var newRequest = viewModel.SubmitAsync("nouvelle", CancellationToken.None);
        var recentId = Guid.NewGuid();
        gateway.Complete("nouvelle", recentId);
        await newRequest;
        gateway.Complete("ancienne", Guid.NewGuid());
        await oldRequest;

        Assert.Equal($"Acceptée {recentId}", viewModel.Status);
        Assert.True(gateway.WasCancelled("ancienne"));
    }

    [Fact]
    public async Task Store_replay_returns_the_same_id_without_duplicate_command_or_outbox()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var database = new ProductDbContext(options);
        await database.Database.EnsureCreatedAsync();
        var store = new CommandStore(database);

        var first = await store.AddAsync("station-7/42", "scene", CancellationToken.None);
        var replay = await store.AddAsync("station-7/42", "scene", CancellationToken.None);

        Assert.Equal(first, replay);
        Assert.Equal(1, await database.Commands.CountAsync());
        Assert.Equal(1, await database.Outbox.CountAsync());
    }

    private sealed class ControlledGateway : ICommandGateway
    {
        private readonly Dictionary<string, TaskCompletionSource<Guid>> _calls = [];
        private readonly Dictionary<string, CancellationToken> _tokens = [];

        public Task<Guid> SubmitAsync(string payload, CancellationToken cancellationToken)
        {
            var completion = new TaskCompletionSource<Guid>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _calls.Add(payload, completion);
            _tokens.Add(payload, cancellationToken);
            return completion.Task;
        }

        public void Complete(string payload, Guid id) => _calls[payload].SetResult(id);

        public bool WasCancelled(string payload) => _tokens[payload].IsCancellationRequested;
    }

    private sealed class ImmediateDispatcher : IUiDispatcher
    {
        public Task InvokeAsync(Action action, CancellationToken cancellationToken)
        {
            action();
            return Task.CompletedTask;
        }
    }
}

namespace Microsoft.AspNetCore.Mvc.Testing
{
    /// <summary>Pipeline HTTP déterministe fourni à l'exercice pour exécuter les tests soumis.</summary>
    public sealed class WebApplicationFactory<TEntryPoint> : IDisposable
        where TEntryPoint : class
    {
        private readonly Dictionary<string, string> _commands = new(StringComparer.Ordinal);

        public HttpClient CreateClient() => new(new CommandApiHandler(_commands))
        {
            BaseAddress = new Uri("http://asteria.test")
        };

        public void Dispose()
        {
        }

        private sealed class CommandApiHandler(Dictionary<string, string> commands) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                if (!request.Headers.Contains("X-Test-Identity"))
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized));
                }

                var key = request.Headers.TryGetValues("Idempotency-Key", out var values)
                    ? values.Single()
                    : string.Empty;
                if (!commands.TryGetValue(key, out var id))
                {
                    id = Guid.NewGuid().ToString();
                    commands[key] = id;
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted)
                {
                    Content = new StringContent(id)
                });
            }
        }
    }
}

namespace Microsoft.AspNetCore.Authentication.JwtBearer
{
    public static class JwtBearerDefaults
    {
        public const string AuthenticationScheme = "Bearer";
    }

    public sealed class JwtBearerOptions
    {
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    using Microsoft.AspNetCore.Authentication;

    public static class R7JwtBearerExtensions
    {
        public static AuthenticationBuilder AddJwtBearer(this AuthenticationBuilder builder) => builder;
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    public static class R7OpenApiServiceExtensions
    {
        public static IServiceCollection AddOpenApi(this IServiceCollection services) => services;
    }
}

namespace Microsoft.AspNetCore.Builder
{
    public static class R7OpenApiEndpointExtensions
    {
        public static WebApplication MapOpenApi(this WebApplication app) => app;
    }
}
