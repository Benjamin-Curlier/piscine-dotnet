using System;
using System.IO;
using Piscine.Core.Progression;
using Xunit;

namespace Piscine.Core.Tests;

public sealed class LastCheckFailureStoreTests
{
    [Fact]
    public void SaveThenLoad_RoundTripsSnapshot()
    {
        using var dir = new TempDir();
        var path = dir.Combine(Path.Combine("state", "last-check-failure.json"));
        var expected = new LastCheckFailure("ex03", DateTimeOffset.UnixEpoch, "Entrée du test : \"42\\n\"");
        var store = new LastCheckFailureStore(path);

        store.Save(expected);

        Assert.Equal(expected, store.Load());
    }

    [Fact]
    public void Load_CorruptedJson_ReturnsNull()
    {
        using var dir = new TempDir();
        var path = dir.Combine(Path.Combine("state", "last-check-failure.json"));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "{cassé");

        Assert.Null(new LastCheckFailureStore(path).Load());
    }
}
