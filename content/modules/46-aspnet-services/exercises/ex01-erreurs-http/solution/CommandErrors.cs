using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

public enum CommandError
{
    Validation,
    NotFound,
    Conflict
}

public static class CommandErrors
{
    public static Results<ProblemHttpResult, Ok> ToHttp(CommandError error, string traceId)
    {
        var extensions = new Dictionary<string, object?> { ["traceId"] = traceId };
        return error switch
        {
            CommandError.Validation => TypedResults.Problem(
                "Commande invalide", statusCode: StatusCodes.Status400BadRequest, extensions: extensions),
            CommandError.NotFound => TypedResults.Problem(
                "Commande inconnue", statusCode: StatusCodes.Status404NotFound, extensions: extensions),
            CommandError.Conflict => TypedResults.Problem(
                "Version en conflit", statusCode: StatusCodes.Status409Conflict, extensions: extensions),
            _ => TypedResults.Problem(statusCode: StatusCodes.Status500InternalServerError, extensions: extensions)
        };
    }
}
