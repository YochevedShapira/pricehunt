using Microsoft.Extensions.DependencyInjection;
using PriceHunt.Application.Suppliers;
using PriceHunt.Infrastructure.Suppliers;

namespace PriceHunt.Infrastructure.Tests.Suppliers;

public class SupplierRegistrationTests
{
    private ISupplierCatalog _catalog = null!;

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();
        services.AddSimulatedSuppliers();
        _catalog = services.BuildServiceProvider().GetRequiredService<ISupplierCatalog>();
    }

    [Test]
    public void AddSimulatedSuppliers_RegistersSevenSuppliers()
    {
        Assert.That(_catalog.All, Has.Count.EqualTo(7));
    }

    [Test]
    public void AddSimulatedSuppliers_RegistersDistinctNames()
    {
        Assert.That(_catalog.All.Select(s => s.Name), Is.Unique);
    }

    [Test]
    public void AddSimulatedSuppliers_ResolvesEverySupplierByName()
    {
        Assert.That(_catalog.All.Select(s => _catalog.Find(s.Name)), Has.All.Not.Null);
    }
}
