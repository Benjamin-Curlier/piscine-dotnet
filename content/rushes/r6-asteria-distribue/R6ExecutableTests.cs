global using System;
global using System.Collections.Generic;
global using System.Linq;
global using Microsoft.AspNetCore.Builder;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Hosting;

using Microsoft.AspNetCore.Routing;
using Xunit;

public sealed class R6ExecutableTests
{
    [Fact]
    public void Service_defaults_register_the_cross_cutting_stack_and_health_routes()
    {
        R6Recorder.Reset();
        var builder = WebApplication.CreateBuilder();

        builder.AddServiceDefaults();
        var app = builder.Build();
        app.MapDefaultEndpoints();

        var routes = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(endpoint => endpoint.RoutePattern.RawText)
            .ToList();
        Assert.True(R6Recorder.OpenTelemetry);
        Assert.True(R6Recorder.Metrics);
        Assert.True(R6Recorder.Tracing);
        Assert.True(R6Recorder.ServiceDiscovery);
        Assert.True(R6Recorder.Resilience);
        Assert.Contains("/health", routes);
        Assert.Contains("/alive", routes);
    }
}

public static class R6Recorder
{
    public static bool OpenTelemetry { get; set; }
    public static bool Metrics { get; set; }
    public static bool Tracing { get; set; }
    public static bool ServiceDiscovery { get; set; }
    public static bool Resilience { get; set; }

    public static void Reset()
    {
        OpenTelemetry = false;
        Metrics = false;
        Tracing = false;
        ServiceDiscovery = false;
        Resilience = false;
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    public static class R6ServiceCollectionExtensions
    {
        public static R6TelemetryBuilder AddOpenTelemetry(this IServiceCollection services)
        {
            R6Recorder.OpenTelemetry = true;
            return new R6TelemetryBuilder();
        }

        public static IServiceCollection AddServiceDiscovery(this IServiceCollection services)
        {
            R6Recorder.ServiceDiscovery = true;
            return services;
        }

        public static IServiceCollection ConfigureHttpClientDefaults(
            this IServiceCollection services,
            Action<R6HttpClientBuilder> configure)
        {
            configure(new R6HttpClientBuilder());
            return services;
        }
    }

    public sealed class R6TelemetryBuilder
    {
        public R6TelemetryBuilder WithMetrics()
        {
            R6Recorder.Metrics = true;
            return this;
        }

        public R6TelemetryBuilder WithTracing()
        {
            R6Recorder.Tracing = true;
            return this;
        }
    }

    public sealed class R6HttpClientBuilder
    {
        public R6HttpClientBuilder AddStandardResilienceHandler()
        {
            R6Recorder.Resilience = true;
            return this;
        }

        public R6HttpClientBuilder AddServiceDiscovery()
        {
            R6Recorder.ServiceDiscovery = true;
            return this;
        }
    }
}

public static class DistributedApplication
{
    public static R6DistributedBuilder CreateBuilder(string[] args) => new();
}

public sealed class R6DistributedBuilder
{
    public R6ContainerBuilder AddContainer(string name, string image, string tag) => new();
    public R6ProjectBuilder AddProject<TProject>(string name) => new();
    public R6DistributedApplication Build() => new();
}

public sealed class R6ContainerBuilder
{
    public R6ContainerBuilder WithArgs(string argument) => this;
    public R6ContainerBuilder WithDataVolume() => this;
}

public sealed class R6ProjectBuilder
{
    public R6ProjectBuilder WithReference(R6ContainerBuilder resource) => this;
    public R6ProjectBuilder WaitFor(R6ContainerBuilder resource) => this;
    public R6ProjectBuilder WithHttpHealthCheck(string path) => this;
}

public sealed class R6DistributedApplication
{
    public void Run()
    {
    }
}

namespace Projects
{
    public sealed class Api
    {
    }

    public sealed class Worker
    {
    }
}
