namespace PriceHunt.Application.Suppliers;

/// <summary>
/// Raised by an <see cref="ISupplier"/> when it cannot produce a price.
/// The fan-out turns it into a failed outcome for that supplier only.
/// </summary>
public sealed class SupplierUnavailableException : Exception
{
    public SupplierUnavailableException(string supplierName, string message)
        : base(message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(supplierName);
        SupplierName = supplierName;
    }

    public SupplierUnavailableException(
        string supplierName,
        string message,
        Exception innerException)
        : base(message, innerException)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(supplierName);
        SupplierName = supplierName;
    }

    public string SupplierName { get; }
}
