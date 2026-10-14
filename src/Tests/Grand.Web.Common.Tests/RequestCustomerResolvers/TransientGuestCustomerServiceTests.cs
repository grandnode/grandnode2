using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Web.Common.RequestCustomerResolvers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq.Expressions;

namespace Grand.Web.Common.Tests.RequestCustomerResolvers;

[TestClass]
public class TransientGuestCustomerServiceTests
{
    private Mock<ICustomerService> _inner;
    private Mock<IGuestCustomerPersistence> _persistence;
    private TransientGuestCustomerService _service;
    private readonly Customer _transient = new() { Id = "transient" };

    [TestInitialize]
    public void Setup()
    {
        _inner = new Mock<ICustomerService>();
        _persistence = new Mock<IGuestCustomerPersistence>();
        _persistence.Setup(p => p.IsTransient("transient")).Returns(true);
        _persistence.SetupGet(p => p.Transient).Returns(_transient);
        _persistence.Setup(p => p.EnsurePersisted()).Returns(Task.CompletedTask);
        _service = new TransientGuestCustomerService(_inner.Object, _persistence.Object);
    }

    [TestMethod]
    public async Task UpdateCustomerFieldById_OnTransient_PersistsThenWrites()
    {
        //AffiliateAttribute writes the affiliate id by customer id on the landing GET
        var order = new List<string>();
        _persistence.Setup(p => p.EnsurePersisted()).Callback(() => order.Add("persist")).Returns(Task.CompletedTask);
        _inner.Setup(i => i.UpdateCustomerField("transient", It.IsAny<Expression<Func<Customer, string>>>(), "aff"))
            .Callback(() => order.Add("write")).Returns(Task.CompletedTask);

        await _service.UpdateCustomerField("transient", x => x.AffiliateId, "aff");

        CollectionAssert.AreEqual(new[] { "persist", "write" }, order);
    }

    [TestMethod]
    public async Task UpdateUserField_OnTransient_Persists()
    {
        await _service.UpdateUserField(_transient, SystemCustomerFieldNames.LanguageId, "pl", "store");

        _persistence.Verify(p => p.EnsurePersisted(), Times.Once);
        _inner.Verify(i => i.UpdateUserField(_transient, SystemCustomerFieldNames.LanguageId, "pl", "store"), Times.Once);
    }

    [TestMethod]
    public async Task InsertShoppingCartItem_OnTransient_Persists()
    {
        await _service.InsertShoppingCartItem("transient", new ShoppingCartItem());

        _persistence.Verify(p => p.EnsurePersisted(), Times.Once);
    }

    [TestMethod]
    public async Task Write_OnAnotherCustomer_DoesNotPersist()
    {
        await _service.UpdateUserField(new Customer { Id = "other" }, "key", "value");
        await _service.UpdateCustomerField("other", x => x.AffiliateId, "aff");

        _persistence.Verify(p => p.EnsurePersisted(), Times.Never);
    }

    [TestMethod]
    public async Task Reads_NeverPersist()
    {
        await _service.GetCustomerByGuid(Guid.NewGuid());
        await _service.GetCustomerBySystemName("x");

        _persistence.Verify(p => p.EnsurePersisted(), Times.Never);
    }

    [TestMethod]
    public async Task GetCustomerById_OfTransient_ReturnsTransientWithoutQuery()
    {
        Assert.AreSame(_transient, await _service.GetCustomerById("transient"));
        _inner.Verify(i => i.GetCustomerById(It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task InsertGuestCustomer_PassesThrough()
    {
        var guest = new Customer();
        _inner.Setup(i => i.InsertGuestCustomer(guest)).ReturnsAsync(guest);

        Assert.AreSame(guest, await _service.InsertGuestCustomer(guest));
        _persistence.Verify(p => p.EnsurePersisted(), Times.Never);
    }

    [TestMethod]
    public void Registration_DecoratesCustomerServiceAndSharesPersistencePerScope()
    {
        var services = new ServiceCollection();
        services.AddScoped<ICustomerService, FakeCustomerService>();
        services.AddSingleton(Mock.Of<IGroupService>());
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        services.AddTransientGuestCustomers();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();
        Assert.AreEqual(typeof(TransientGuestCustomerService),
            scope.ServiceProvider.GetRequiredService<ICustomerService>().GetType());
        Assert.AreSame(scope.ServiceProvider.GetRequiredService<IGuestCustomerPersistence>(),
            scope.ServiceProvider.GetRequiredService<IGuestCustomerPersistence>());
        Assert.AreEqual(1, services.Count(d => d.ServiceType == typeof(ICustomerService)));
    }

    [TestMethod]
    public void Registration_NoCustomerService_Throws()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => new ServiceCollection().AddTransientGuestCustomers());
    }

    [TestMethod]
    public void Startup_RunsInSecondPass()
    {
        //second pass, after Grand.Business.Customers registers ICustomerService
        Assert.IsFalse(new Grand.Web.Common.Startup.GuestCustomerStartup().BeforeConfigure);
    }

    //any concrete ICustomerService with a parameterless constructor, as the business layer registers one
    private class FakeCustomerService()
        : TransientGuestCustomerService(Mock.Of<ICustomerService>(), Mock.Of<IGuestCustomerPersistence>());
}
