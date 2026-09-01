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
    public override string Handle(Request request) =>
        request.Role is "operateur" or "admin" ? Next(request) : "REFUS authentification";
}

sealed class AmountHandler : Handler
{
    public override string Handle(Request request) =>
        request.Amount > 0 ? Next(request) : "REFUS montant";
}

sealed class LimitHandler : Handler
{
    public override string Handle(Request request) =>
        request.Role == "admin" || request.Amount <= 100 ? Next(request) : "REFUS limite";
}

sealed class ExecutionHandler : Handler
{
    public override string Handle(Request request) => "ACCEPTE";
}
