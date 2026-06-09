if (args.Length < 1)
{
    System.Console.Error.WriteLine("usage: Piscine.Sandbox <workdir>");
    return 64;
}

return Piscine.Sandbox.SandboxEntry.Run(args[0]);
