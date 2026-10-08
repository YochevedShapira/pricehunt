namespace PriceHunt.Application.Searching.Events;

/// <summary>
/// Base of the closed set of events a search emits while it runs. The
/// stream always starts with <see cref="SearchStarted"/> and ends with
/// <see cref="SearchEnded"/>; between them come zero or more
/// <see cref="QuoteReceived"/> and <see cref="SupplierFailed"/> events.
/// </summary>
public abstract record SearchEvent;
