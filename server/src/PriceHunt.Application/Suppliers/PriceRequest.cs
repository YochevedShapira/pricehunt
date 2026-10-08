using PriceHunt.Domain.ValueObjects;

namespace PriceHunt.Application.Suppliers;

/// <summary>
/// What a supplier needs to produce a price.
/// </summary>
public sealed record PriceRequest(Route Route, DateRange Dates);
