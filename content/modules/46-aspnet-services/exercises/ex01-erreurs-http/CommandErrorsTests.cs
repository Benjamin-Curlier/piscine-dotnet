global using System.Collections.Generic;

using Microsoft.AspNetCore.Http.HttpResults;
using Xunit;

public sealed class CommandErrorsTests
{
    [Theory]
    [InlineData(CommandError.Validation, 400)]
    [InlineData(CommandError.NotFound, 404)]
    [InlineData(CommandError.Conflict, 409)]
    public void Each_domain_error_has_a_stable_status_and_trace(
        CommandError error,
        int expectedStatus)
    {
        var response = CommandErrors.ToHttp(error, "trace-42");

        var problem = Assert.IsType<ProblemHttpResult>(response.Result);
        Assert.Equal(expectedStatus, problem.StatusCode);
        Assert.Equal("trace-42", problem.ProblemDetails.Extensions["traceId"]);
    }
}
