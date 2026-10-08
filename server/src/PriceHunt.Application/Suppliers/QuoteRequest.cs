using PriceHunt.Domain.ValueObjects;

namespace PriceHunt.Application.Suppliers;

/// <summary>
/// What a supplier needs to produce a price.
/// </summary>
public sealed record QuoteRequest(Route Route, DateRange Dates);
