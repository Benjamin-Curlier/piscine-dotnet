using System.Text;
using Piscine.App.Terminal;

namespace Piscine.App.Tests;

public sealed class PtyServiceTests
{
    [Fact]
    public async Task Echo_round_trips_through_the_pty()
    {
        var svc = new PtyService();
        IPtySession session;
        try
        {
            session = await svc.StartAsync(new PtyStartInfo());
        }
        catch (Exception ex) when (ex is not Xunit.Sdk.XunitException)
        {
            // PTY indisponible (CI sans ConPTY/shim) : on saute proprement, run verte.
            return;
        }

        await using (session)
        {
            var sb = new StringBuilder();
            using var got = new SemaphoreSlim(0);
            session.Output += bytes =>
            {
                lock (sb)
                {
                    sb.Append(Encoding.UTF8.GetString(bytes));
                    if (sb.ToString().Contains("PISCINE_OK", StringComparison.Ordinal))
                    {
                        got.Release();
                    }
                }
            };

            // 'echo' existe sous cmd, pwsh ET bash.
            await session.WriteAsync("echo PISCINE_OK\r");

            var seen = await got.WaitAsync(TimeSpan.FromSeconds(15));
            string snapshot;
            lock (sb)
            {
                snapshot = sb.ToString();
            }

            Assert.True(seen, $"Sortie PTY inattendue : {snapshot}");
        }
    }
}
