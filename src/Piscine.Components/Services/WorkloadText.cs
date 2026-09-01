namespace Piscine.Components.Services;

/// <summary>Formate les durées pédagogiques cumulées sans masquer les minutes résiduelles.</summary>
public static class WorkloadText
{
    public static string Format(int minutes)
    {
        if (minutes < 60)
        {
            return $"{minutes} min";
        }

        var hours = minutes / 60;
        var remaining = minutes % 60;
        return remaining == 0 ? $"{hours} h" : $"{hours} h {remaining:00}";
    }
}
