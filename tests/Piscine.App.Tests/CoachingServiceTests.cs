using System.Linq;
using Piscine.App.Coaching;
using Piscine.App.Git;

namespace Piscine.App.Tests;

/// <summary>
/// Tests du <see cref="CoachingService"/> : un <c>[Fact]</c> par regle du tableau spec section 5,
/// plus l'etat propre (0 carte) et l'ordre de priorite. Les <see cref="RepoState"/> sont construits
/// a la main (le service est pur : aucun depot reel necessaire).
/// </summary>
public sealed class CoachingServiceTests
{
    private static readonly CoachingService Coach = new();

    /// <summary>Etat de reference « propre et pousse » : depot valide, origin present, branche, commit, rien en attente.</summary>
    private static RepoState CleanPushed() => new()
    {
        IsRepository = true,
        HasOrigin = true,
        CurrentBranch = "main",
        HasAnyCommit = true,
        AheadOfOrigin = 0,
    };

    private static bool Has(System.Collections.Generic.IReadOnlyList<HintCard> cards, string id) =>
        cards.Any(c => c.Id == id);

    // --- Regle 1 : init / origin manquant -----------------------------------------------------

    [Fact]
    public void Evaluate_NotARepository_EmitsInitMissingBlock()
    {
        // Arrange
        var state = new RepoState { IsRepository = false };

        // Act
        var cards = Coach.Evaluate(state, lastCommand: null, new ExerciseExpectation());

        // Assert
        var card = Assert.Single(cards, c => c.Id == "init_missing");
        Assert.Equal(HintSeverity.Block, card.Severity);
    }

    [Fact]
    public void Evaluate_RepositoryWithoutOrigin_EmitsOriginMissingBlock()
    {
        // Arrange
        var state = new RepoState
        {
            IsRepository = true,
            HasOrigin = false,
            CurrentBranch = "main",
            HasAnyCommit = true,
        };

        // Act
        var cards = Coach.Evaluate(state, lastCommand: null, new ExerciseExpectation());

        // Assert
        var card = Assert.Single(cards, c => c.Id == "origin_missing");
        Assert.Equal(HintSeverity.Block, card.Severity);
    }

    // --- Regle 2 : marqueurs de conflit -------------------------------------------------------

    [Fact]
    public void Evaluate_ConflictedFiles_EmitsConflictMarkersBlock()
    {
        // Arrange
        var state = CleanPushed() with { ConflictedFiles = ["src/Foo.cs"] };

        // Act
        var cards = Coach.Evaluate(state, lastCommand: null, new ExerciseExpectation());

        // Assert
        var card = Assert.Single(cards, c => c.Id == "conflict_markers");
        Assert.Equal(HintSeverity.Block, card.Severity);
    }

    // --- Regle 3 : HEAD detache ---------------------------------------------------------------

    [Fact]
    public void Evaluate_DetachedHead_EmitsDetachedHeadWarn()
    {
        // Arrange
        var state = new RepoState
        {
            IsRepository = true,
            HasOrigin = true,
            HasAnyCommit = true,
            IsDetachedHead = true,
            CurrentBranch = null,
        };

        // Act
        var cards = Coach.Evaluate(state, lastCommand: null, new ExerciseExpectation());

        // Assert
        var card = Assert.Single(cards, c => c.Id == "detached_head");
        Assert.Equal(HintSeverity.Warn, card.Severity);
    }

    // --- Regle 4 : mauvaise branche -----------------------------------------------------------

    [Fact]
    public void Evaluate_WrongBranch_EmitsWrongBranchWarn()
    {
        // Arrange
        var state = CleanPushed() with { CurrentBranch = "wip" };
        var expectation = new ExerciseExpectation { ExpectedBranch = "main" };

        // Act
        var cards = Coach.Evaluate(state, lastCommand: null, expectation);

        // Assert
        var card = Assert.Single(cards, c => c.Id == "wrong_branch");
        Assert.Equal(HintSeverity.Warn, card.Severity);
    }

    // --- Regle 5 : commit sans rien stage -----------------------------------------------------

    [Fact]
    public void Evaluate_CommitWithNothingStaged_EmitsNothingStagedWarn()
    {
        // Arrange
        var state = CleanPushed() with { StagedCount = 0 };
        var evt = new GitCommandEvent(["commit", "-m", "wip"], ExitCode: 1, Cwd: "/repo");

        // Act
        var cards = Coach.Evaluate(state, evt, new ExerciseExpectation());

        // Assert
        var card = Assert.Single(cards, c => c.Id == "commit_nothing_staged");
        Assert.Equal(HintSeverity.Warn, card.Severity);
    }

    // --- Regle 6 : typo de sous-commande ------------------------------------------------------

    [Fact]
    public void Evaluate_TypoSubcommand_EmitsTypoInfo()
    {
        // Arrange : « comit » est a distance 1 de « commit » et a echoue.
        var state = CleanPushed();
        var evt = new GitCommandEvent(["comit", "-m", "x"], ExitCode: 1, Cwd: "/repo");

        // Act
        var cards = Coach.Evaluate(state, evt, new ExerciseExpectation());

        // Assert
        var card = Assert.Single(cards, c => c.Id == "typo_subcommand");
        Assert.Equal(HintSeverity.Info, card.Severity);
        Assert.Contains("commit", card.Message, System.StringComparison.Ordinal);
    }

    // --- Regle 7 : commite mais pas pousse ----------------------------------------------------

    [Fact]
    public void Evaluate_CommittedNotPushed_EmitsCommittedNotPushedInfo()
    {
        // Arrange : un commit local en avance sur origin, sans evenement de commande.
        var state = CleanPushed() with { AheadOfOrigin = 2 };

        // Act
        var cards = Coach.Evaluate(state, lastCommand: null, new ExerciseExpectation());

        // Assert
        var card = Assert.Single(cards, c => c.Id == "committed_not_pushed");
        Assert.Equal(HintSeverity.Info, card.Severity);
    }

    // --- Regle 8 : pousse mais correction KO --------------------------------------------------

    [Fact]
    public void Evaluate_GradeReceivedFailed_EmitsGradeFailedWarn()
    {
        // Arrange
        var state = CleanPushed();
        var expectation = new ExerciseExpectation { GradeReceivedFailed = true };

        // Act
        var cards = Coach.Evaluate(state, lastCommand: null, expectation);

        // Assert
        var card = Assert.Single(cards, c => c.Id == "grade_received_failed");
        Assert.Equal(HintSeverity.Warn, card.Severity);
    }

    // --- Etat propre : aucune carte -----------------------------------------------------------

    [Fact]
    public void Evaluate_CleanPushedState_EmitsNoCards()
    {
        // Arrange
        var state = CleanPushed();

        // Act
        var cards = Coach.Evaluate(state, lastCommand: null, new ExerciseExpectation());

        // Assert
        Assert.Empty(cards);
    }

    // --- Ordre de priorite : conflit (Block) avant commite-non-pousse (Info) ------------------

    [Fact]
    public void Evaluate_ConflictAndAhead_OrdersConflictBeforeCommittedNotPushed()
    {
        // Arrange : conflit ET commits en avance -> deux cartes attendues, conflit en premier.
        var state = CleanPushed() with
        {
            ConflictedFiles = ["src/Foo.cs"],
            AheadOfOrigin = 1,
        };

        // Act
        var cards = Coach.Evaluate(state, lastCommand: null, new ExerciseExpectation());

        // Assert
        Assert.True(Has(cards, "conflict_markers"));
        Assert.True(Has(cards, "committed_not_pushed"));
        var conflictIndex = cards.ToList().FindIndex(c => c.Id == "conflict_markers");
        var pushIndex = cards.ToList().FindIndex(c => c.Id == "committed_not_pushed");
        Assert.True(conflictIndex < pushIndex, "Le conflit (Block) doit preceder « commite non pousse » (Info).");
    }
}
