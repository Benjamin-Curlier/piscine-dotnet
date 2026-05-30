using Piscine.Git;
using Xunit;

namespace Piscine.Git.Tests;

public class HookScriptTests
{
    [Fact]
    public void PostReceive_InvokesPiscineGradeReceivedForEachRef()
    {
        var script = HookScript.PostReceive(@"C:\piscine\piscine.exe");

        Assert.StartsWith("#!/bin/sh", script);
        Assert.Contains("while read", script);
        Assert.Contains("grade-received", script);
        Assert.Contains("$newrev", script);
        // Chemin normalisé pour sh (pas d'antislash).
        Assert.Contains("C:/piscine/piscine.exe", script);
        Assert.DoesNotContain("\\", script);
        // Fin de ligne LF uniquement (hook lancé par sh).
        Assert.DoesNotContain("\r", script);
    }
}
