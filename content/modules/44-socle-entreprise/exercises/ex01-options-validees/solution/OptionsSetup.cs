using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public sealed class StationOptions
{
    public const string SectionName = "Station";

    [Required]
    public required string StationId { get; init; }

    [Range(1, 120)]
    public int TimeoutSeconds { get; init; } = 15;
}

public static class OptionsSetup
{
    public static IServiceCollection AddStationOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<StationOptions>()
            .Bind(configuration.GetSection(StationOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
