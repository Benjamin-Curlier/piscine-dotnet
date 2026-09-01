global using System;
global using System.Collections.Generic;
global using System.Threading;
global using System.Threading.Tasks;

using Xunit;

public sealed class LatestSearchViewModelTests
{
    [Fact]
    public async Task Latest_result_wins_even_when_the_previous_call_finishes_after_it()
    {
        var client = new ControlledSearchClient();
        var viewModel = new LatestSearchViewModel(client);

        var oldSearch = viewModel.SearchAsync("ancienne", CancellationToken.None);
        var newSearch = viewModel.SearchAsync("nouvelle", CancellationToken.None);

        client.Complete("nouvelle", ["résultat récent"]);
        await newSearch;
        client.Complete("ancienne", ["résultat obsolète"]);
        await oldSearch;

        Assert.Equal(["résultat récent"], viewModel.Results);
        Assert.True(client.WasCancelled("ancienne"));
    }

    private sealed class ControlledSearchClient : ISearchClient
    {
        private readonly Dictionary<string, TaskCompletionSource<IReadOnlyList<string>>> _calls = [];
        private readonly Dictionary<string, CancellationToken> _tokens = [];

        public Task<IReadOnlyList<string>> SearchAsync(string query, CancellationToken cancellationToken)
        {
            var completion = new TaskCompletionSource<IReadOnlyList<string>>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            _calls.Add(query, completion);
            _tokens.Add(query, cancellationToken);
            return completion.Task;
        }

        public void Complete(string query, IReadOnlyList<string> results) =>
            _calls[query].SetResult(results);

        public bool WasCancelled(string query) => _tokens[query].IsCancellationRequested;
    }
}
