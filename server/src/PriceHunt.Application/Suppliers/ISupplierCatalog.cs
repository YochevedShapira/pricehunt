namespace PriceHunt.Application.Suppliers;

/// <summary>
/// The set of suppliers the system can query.
/// </summary>
public interface ISupplierCatalog
{
    /// <summary>
    /// Every known supplier, in display order.
    /// </summary>
    IReadOnlyList<ISupplier> All { get; }

    /// <summary>
    /// Returns the supplier with the given name, or <c>null</c> when the
    /// name is unknown.
    /// </summary>
    ISupplier? Find(string name);
}
