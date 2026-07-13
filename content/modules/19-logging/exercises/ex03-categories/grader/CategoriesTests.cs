using Microsoft.Extensions.Logging;
using Xunit;

// Tests cachés (grader unit) : « Réussi » exige un vrai filtrage par catégorie (via AddFilter) —
// pas trois Console.WriteLine des lignes attendues. On interroge directement IsEnabled.
public class CategoriesTests
{
    [Fact]
    public void Db_NEmetPasEnInformation_MaisEmetEnWarning()
    {
        using var fabrique = Journalisation.CreerFabrique();
        var db = fabrique.CreateLogger("Db");

        Assert.False(db.IsEnabled(LogLevel.Information), "La catégorie Db ne doit émettre qu'à partir de Warning.");
        Assert.True(db.IsEnabled(LogLevel.Warning), "La catégorie Db doit émettre en Warning.");
    }

    [Fact]
    public void App_EmetDesLInformation()
    {
        using var fabrique = Journalisation.CreerFabrique();
        var app = fabrique.CreateLogger("App");

        Assert.True(app.IsEnabled(LogLevel.Information), "La catégorie App doit émettre dès Information.");
    }
}
