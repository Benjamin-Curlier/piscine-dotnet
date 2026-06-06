using Piscine.Grading;
using Xunit;

namespace Piscine.Grading.Tests;

public class MutationGraderPatchTests
{
    [Fact]
    public void ApplyPatch_ReplacesSingleOccurrence()
    {
        var (result, count) = MutationGrader.ApplyPatch("if (a > b) return;", "> b", ">= b");

        Assert.Equal(1, count);
        Assert.Equal("if (a >= b) return;", result);
    }

    [Fact]
    public void ApplyPatch_ReturnsZero_WhenNotFound()
    {
        var (result, count) = MutationGrader.ApplyPatch("x = 1;", "absent", "autre");

        Assert.Equal(0, count);
        Assert.Equal("x = 1;", result);
    }

    [Fact]
    public void ApplyPatch_ReturnsTwo_WhenAmbiguous()
    {
        var (_, count) = MutationGrader.ApplyPatch("a + a", "a", "b");

        Assert.Equal(2, count);
    }

    [Fact]
    public void ApplyPatch_ReturnsZero_WhenFindEmpty()
    {
        var (result, count) = MutationGrader.ApplyPatch("abc", "", "x");

        Assert.Equal(0, count);
        Assert.Equal("abc", result);
    }
}
