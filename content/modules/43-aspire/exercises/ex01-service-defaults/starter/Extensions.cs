public static class Extensions
{
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        // TODO : compose télémétrie, santé, découverte et résilience HTTP.
        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        // TODO : expose readiness et liveness sur deux routes distinctes.
        return app;
    }
}
