namespace Piscine.Grading;

/// <summary>Fabrique l'ensemble standard de graders de la piscine.</summary>
public static class Graders
{
    public static ExerciseGrader Default() =>
        new(new IGrader[] { new IoGrader(), new NormeGrader(), new UnitGrader(), new MutationGrader() });
}
