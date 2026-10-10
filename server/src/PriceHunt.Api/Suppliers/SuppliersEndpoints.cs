using Microsoft.AspNetCore.Http.HttpResults;
using PriceHunt.Application.Suppliers;

namespace PriceHunt.Api.Suppliers;

/// <summary>
/// <c>GET /api/suppliers</c>: the catalog, so the client never hard-codes
/// supplier names.
/// </summary>
public static class SuppliersEndpoints
{
    public static IEndpointRouteBuilder MapSuppliers(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/suppliers", List)
            .WithName("ListSuppliers")
            .WithSummary("Lists the suppliers that can be queried.");

        return endpoints;
    }

    private static Ok<IReadOnlyList<SupplierDto>> List(ISupplierCatalog catalog)
    {
        IReadOnlyList<SupplierDto> suppliers = catalog.All
            .Select(s => new SupplierDto(s.Name))
            .ToList();

        return TypedResults.Ok(suppliers);
    }
}
