namespace PriceHunt.Application.Searching;

/// <summary>
/// Tunable settings of a price search. The application layer receives this
/// as an already-built object; reading it from configuration is the host's
/// job, so this layer knows nothing about appsettings or section names.
/// </summary>
public sealed class SearchOptions
{
    /// <summary>
    /// Maximum duration of a search. When it elapses the search ends with the
    /// results collected so far. Defaults to the brief's six seconds.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(6);
}
