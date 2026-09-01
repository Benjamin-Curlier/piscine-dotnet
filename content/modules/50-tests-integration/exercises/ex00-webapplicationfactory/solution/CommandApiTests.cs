using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

public sealed class CommandApiTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task ValidCommand_ReturnsAccepted()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Identity", "operator");
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var response = await client.PostAsJsonAsync(
            "/commands",
            new { kind = "render", payload = "scene-42" },
            cancellation.Token);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
    }

    [Fact]
    public async Task InvalidCommand_ReturnsBadRequest()
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Identity", "operator");
        var response = await client.PostAsJsonAsync("/commands", new { kind = "", payload = "" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AnonymousCommand_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/commands", new { kind = "render", payload = "x" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
