public interface ISearchClient
{
    Task<IReadOnlyList<string>> SearchAsync(string query, CancellationToken cancellationToken);
}

// TODO: dernière requête gagnante.
