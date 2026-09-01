global using System;
global using System.Net.Http;
global using System.Threading;
global using System.Threading.Tasks;

using System.Net;

public sealed class Program
{
}

namespace Microsoft.AspNetCore.Mvc.Testing
{
    /// <summary>
    /// Hôte déterministe fourni par la moulinette. Il expose le même point d'entrée que le service
    /// Asteria sans socket réseau ; la soumission doit l'utiliser via WebApplicationFactory.
    /// </summary>
    public sealed class WebApplicationFactory<TEntryPoint> : IDisposable
        where TEntryPoint : class
    {
        public HttpClient CreateClient() => new(new CommandApiHandler())
        {
            BaseAddress = new Uri("http://asteria.test")
        };

        public void Dispose()
        {
        }

        private sealed class CommandApiHandler : HttpMessageHandler
        {
            protected override async Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                if (request.Method != HttpMethod.Post || request.RequestUri?.AbsolutePath != "/commands")
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }

                if (!request.Headers.Contains("X-Test-Identity"))
                {
                    return new HttpResponseMessage(HttpStatusCode.Unauthorized);
                }

                var body = request.Content is null
                    ? string.Empty
                    : await request.Content.ReadAsStringAsync(cancellationToken);
                var status = body.Contains("\"kind\":\"\"", StringComparison.OrdinalIgnoreCase)
                    ? HttpStatusCode.BadRequest
                    : HttpStatusCode.Accepted;
                return new HttpResponseMessage(status);
            }
        }
    }
}
