global using System;
global using System.Collections.Generic;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

public sealed class OptionsSetupTests
{
    [Fact]
    public void Valid_configuration_is_bound_to_typed_options()
    {
        var configuration = BuildConfiguration("station-7", "30");
        var services = new ServiceCollection();

        services.AddStationOptions(configuration);
        using var provider = services.BuildServiceProvider();

        var options = provider.GetRequiredService<IOptions<StationOptions>>().Value;
        Assert.Equal("station-7", options.StationId);
        Assert.Equal(30, options.TimeoutSeconds);
    }

    [Theory]
    [InlineData("", "30")]
    [InlineData("station-7", "0")]
    [InlineData("station-7", "121")]
    public void Invalid_configuration_is_rejected(string stationId, string timeout)
    {
        var configuration = BuildConfiguration(stationId, timeout);
        var services = new ServiceCollection();
        services.AddStationOptions(configuration);
        using var provider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(
            () => _ = provider.GetRequiredService<IOptions<StationOptions>>().Value);
    }

    private static IConfiguration BuildConfiguration(string stationId, string timeout) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Station:StationId"] = stationId,
                ["Station:TimeoutSeconds"] = timeout
            })
            .Build();
}
