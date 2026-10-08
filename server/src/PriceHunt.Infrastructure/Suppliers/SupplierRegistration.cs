using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PriceHunt.Application.Suppliers;

namespace PriceHunt.Infrastructure.Suppliers;

/// <summary>
/// Registers the simulated suppliers and the catalog over them.
/// </summary>
public static class SupplierRegistration
{
    public static IServiceCollection AddSimulatedSuppliers(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);

        // Random.Shared is thread-safe, which matters because one supplier
        // instance serves concurrent searches.
        services.TryAddSingleton(Random.Shared);

        foreach (var profile in SimulatedSupplierProfiles.All)
        {
            services.AddSingleton<ISupplier>(provider => new SimulatedSupplier(
                profile,
                provider.GetRequiredService<TimeProvider>(),
                provider.GetRequiredService<Random>()));
        }

        services.AddSingleton<ISupplierCatalog, SupplierCatalog>();

        return services;
    }
}
