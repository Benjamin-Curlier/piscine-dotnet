using System;

var premier = new AuthenticationHandler();
premier.SetNext(new AmountHandler()).SetNext(new LimitHandler()).SetNext(new ExecutionHandler());

var count = int.Parse(Console.ReadLine()!);
for (var index = 0; index < count; index++)
{
    var parts = Console.ReadLine()!.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var request = new Request(parts[0].ToLowerInvariant(), int.Parse(parts[1]));
    Console.WriteLine(premier.Handle(request));
}

readonly record struct Request(string Role, int Amount);

abstract class Handler
{
    private Handler? _next;

    public Handler SetNext(Handler next)
    {
        _next = next;
        return next;
    }

    protected string Next(Request request) => _next?.Handle(request) ?? "REFUS chaine incomplete";

    public abstract string Handle(Request request);
}

sealed class AuthenticationHandler : Handler
{
    public override string Handle(Request request)
    {
        // TODO : refuse les rôles inconnus, sinon délègue.
        return Next(request);
    }
}

sealed class AmountHandler : Handler
{
    public override string Handle(Request request)
    {
        // TODO : refuse les montants invalides, sinon délègue.
        return Next(request);
    }
}

sealed class LimitHandler : Handler
{
    public override string Handle(Request request)
    {
        // TODO : applique la limite opérateur, sinon délègue.
        return Next(request);
    }
}

sealed class ExecutionHandler : Handler
{
    public override string Handle(Request request) => "ACCEPTE";
}
