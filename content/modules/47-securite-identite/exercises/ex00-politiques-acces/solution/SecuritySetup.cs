using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public static class SecuritySetup
{
    public static IServiceCollection AddAsteriaSecurity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = configuration["Identity:Authority"];
                options.Audience = configuration["Identity:Audience"];
                options.RequireHttpsMetadata = true;
            });

        services.AddAuthorization(options =>
            options.AddPolicy("send-command", policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim("scope", "asteria.command.send")));
        return services;
    }

    public static RouteHandlerBuilder SecureCommandRoute(this RouteHandlerBuilder route) =>
        route.RequireAuthorization("send-command");
}
