using Piscine.Core;
using Xunit;

namespace Piscine.Core.Tests;

public class ExerciseLabelTests
{
    [Fact]
    public void Format_WithDifficulty()
    {
        Assert.Equal("ex00-tri-bulle — facile", ExerciseLabel.Format("ex00-tri-bulle", "facile", bonus: false));
    }

    [Fact]
    public void Format_WithDifficultyAndBonus()
    {
        Assert.Equal("ex03-x — difficile (bonus)", ExerciseLabel.Format("ex03-x", "difficile", bonus: true));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Format_OmitsBlankDifficulty(string? difficulty)
    {
        Assert.Equal("ex00", ExerciseLabel.Format("ex00", difficulty, bonus: false));
    }

    [Fact]
    public void Format_BonusWithoutDifficulty()
    {
        Assert.Equal("ex00 (bonus)", ExerciseLabel.Format("ex00", null, bonus: true));
    }
}
