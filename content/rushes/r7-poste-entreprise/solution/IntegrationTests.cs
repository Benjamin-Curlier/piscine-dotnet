using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public sealed class IntegrationTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task AnonymousCommand_IsRejected()
    {
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/commands", new { kind = "render", payload = "x" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SameIdempotencyKey_ReturnsSameCommand()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Identity", "operator");
        client.DefaultRequestHeaders.Add("Idempotency-Key", "station-7/42");
        var first = await client.PostAsJsonAsync("/commands", new { kind = "render", payload = "scene" });
        var second = await client.PostAsJsonAsync("/commands", new { kind = "render", payload = "scene" });
        Assert.Equal(HttpStatusCode.Accepted, first.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, second.StatusCode);
        var firstId = await first.Content.ReadAsStringAsync();
        var secondId = await second.Content.ReadAsStringAsync();
        Assert.Equal(firstId, secondId);
    }
}
