using PriceHunt.Application.Suppliers;

namespace PriceHunt.Infrastructure.Suppliers;

/// <summary>
/// In-memory <see cref="ISupplierCatalog"/> over the suppliers registered in
/// the container. Names are matched exactly: the client is expected to send
/// back the names it received from the catalog.
/// </summary>
public sealed class SupplierCatalog : ISupplierCatalog
{
    private readonly Dictionary<string, ISupplier> _byName;

    public SupplierCatalog(IEnumerable<ISupplier> suppliers)
    {
        ArgumentNullException.ThrowIfNull(suppliers);

        All = suppliers.ToList();
        if (All.Count == 0)
        {
            throw new ArgumentException(
                "At least one supplier must be registered.", nameof(suppliers));
        }

        try
        {
            _byName = All.ToDictionary(s => s.Name, StringComparer.Ordinal);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentException(
                "Supplier names must be unique.", nameof(suppliers), ex);
        }
    }

    public IReadOnlyList<ISupplier> All { get; }

    public ISupplier? Find(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return _byName.GetValueOrDefault(name);
    }
}
