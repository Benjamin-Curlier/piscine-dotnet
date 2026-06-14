namespace Piscine.DevHost.Qa;

/// <summary>Profils de seed déterministes pour la QA agentique (cf. spec §3.1).</summary>
public enum QaProfile
{
    Fresh,       // workspace NON initialisé → overlay onboarding
    Mixed,       // initialisé, progression variée
    ExoFail,     // un exo avec dernier check en échec (diff)
    ExoPass,     // un exo réussi
    PushResult,  // last-push-result.json récent (toast + /resultat)
    Done,        // quasi tout terminé (rapport significatif)
}

public static class QaProfiles
{
    /// <summary>Parse insensible à la casse/format ("exo-fail" → ExoFail). Null si inconnu.</summary>
    public static QaProfile? Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        var k = raw.Trim().Replace("-", "").Replace("_", "");
        return k.ToLowerInvariant() switch
        {
            "fresh" => QaProfile.Fresh,
            "mixed" => QaProfile.Mixed,
            "exofail" => QaProfile.ExoFail,
            "exopass" => QaProfile.ExoPass,
            "pushresult" => QaProfile.PushResult,
            "done" => QaProfile.Done,
            _ => null,
        };
    }
}
