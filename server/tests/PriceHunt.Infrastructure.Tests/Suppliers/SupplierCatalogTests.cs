using NSubstitute;
using PriceHunt.Application.Suppliers;
using PriceHunt.Infrastructure.Suppliers;

namespace PriceHunt.Infrastructure.Tests.Suppliers;

public class SupplierCatalogTests
{
    private const string OCEANIC = "Oceanic";
    private const string BLUEWAVE = "BlueWave";

    private ISupplier _oceanic = null!;
    private ISupplier _bluewave = null!;
    private SupplierCatalog _catalog = null!;

    [SetUp]
    public void SetUp()
    {
        _oceanic = SupplierNamed(OCEANIC);
        _bluewave = SupplierNamed(BLUEWAVE);
        _catalog = new SupplierCatalog([_oceanic, _bluewave]);
    }

    [Test]
    public void All_PreservesRegistrationOrder()
    {
        Assert.That(_catalog.All, Is.EqualTo(new[] { _oceanic, _bluewave }));
    }

    [Test]
    public void Find_ReturnsTheSupplier_OnAnExactName()
    {
        Assert.That(_catalog.Find(BLUEWAVE), Is.SameAs(_bluewave));
    }

    [Test]
    public void Find_ReturnsNull_OnAnUnknownName()
    {
        Assert.That(_catalog.Find("Nobody"), Is.Null);
    }

    [Test]
    public void Find_ReturnsNull_OnADifferentCase()
    {
        Assert.That(_catalog.Find(OCEANIC.ToUpperInvariant()), Is.Null);
    }

    [Test]
    public void Constructor_Throws_OnDuplicateNames()
    {
        Assert.That(
            () => new SupplierCatalog([_oceanic, SupplierNamed(OCEANIC)]),
            Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void Constructor_Throws_OnEmptyList()
    {
        Assert.That(
            () => new SupplierCatalog([]),
            Throws.InstanceOf<ArgumentException>());
    }

    private static ISupplier SupplierNamed(string name)
    {
        var supplier = Substitute.For<ISupplier>();
        supplier.Name.Returns(name);

        return supplier;
    }
}
