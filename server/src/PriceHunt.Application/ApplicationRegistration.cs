using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PriceHunt.Application.Searching;

namespace PriceHunt.Application;

/// <summary>
/// Registers the use cases of the application layer. The ports they depend
/// on (<c>ISupplierCatalog</c>, <c>ISearchRepository</c>) and the
/// <see cref="SearchOptions"/> value are registered by the host.
/// </summary>
public static class ApplicationRegistration
{
    public static IServiceCollection AddPriceSearch(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<ISupplierFanOut, SupplierFanOut>();

        // Scoped because it works with the request-scoped repository.
        services.AddScoped<IPriceSearch, PriceSearch>();

        return services;
    }
}
