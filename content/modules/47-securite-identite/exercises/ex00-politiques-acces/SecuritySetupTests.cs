global using System;
global using System.Collections.Generic;
global using System.Linq;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

public sealed class SecuritySetupTests
{
    [Fact]
    public void Jwt_and_business_policy_are_configured_from_trusted_configuration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Identity:Authority"] = "https://identity.example",
                ["Identity:Audience"] = "asteria-api"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddAsteriaSecurity(configuration);
        using var provider = services.BuildServiceProvider();

        var jwt = provider.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        Assert.Equal("https://identity.example", jwt.Authority);
        Assert.Equal("asteria-api", jwt.Audience);
        Assert.True(jwt.RequireHttpsMetadata);

        var policy = provider.GetRequiredService<IOptions<AuthorizationOptions>>()
            .Value.GetPolicy("send-command");
        Assert.NotNull(policy);
        Assert.Contains(policy.Requirements, requirement =>
            requirement is DenyAnonymousAuthorizationRequirement);
        Assert.Contains(policy.Requirements, requirement =>
            requirement is ClaimsAuthorizationRequirement claim
            && claim.ClaimType == "scope"
            && claim.AllowedValues is not null
            && claim.AllowedValues.Contains("asteria.command.send"));
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
        public string? Authority { get; set; }
        public string? Audience { get; set; }
        public bool RequireHttpsMetadata { get; set; } = true;
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.JwtBearer;

    public static class JwtBearerExerciseExtensions
    {
        public static AuthenticationBuilder AddJwtBearer(
            this AuthenticationBuilder builder,
            Action<JwtBearerOptions> configure)
        {
            builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure(configure);
            return builder;
        }
    }
}
