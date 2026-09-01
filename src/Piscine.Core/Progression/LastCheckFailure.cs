using System;

namespace Piscine.Core.Progression;

/// <summary>
/// Instantané du dernier échec local. Le texte conserve le diagnostic exact tandis que
/// <see cref="ExerciseId"/> permet de rejouer le même jeu de tests sur le code courant.
/// </summary>
public sealed record LastCheckFailure(string ExerciseId, DateTimeOffset RecordedAt, string Output);
