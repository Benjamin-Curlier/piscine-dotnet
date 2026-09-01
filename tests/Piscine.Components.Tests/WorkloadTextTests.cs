using Piscine.Components.Services;
using Xunit;

namespace Piscine.Components.Tests;

public sealed class WorkloadTextTests
{
    [Theory]
    [InlineData(45, "45 min")]
    [InlineData(60, "1 h")]
    [InlineData(95, "1 h 35")]
    [InlineData(600, "10 h")]
    public void Format_keeps_hours_and_remaining_minutes(int minutes, string expected) =>
        Assert.Equal(expected, WorkloadText.Format(minutes));
}
