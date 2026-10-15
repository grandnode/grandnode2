using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Web.Common.RequestCustomerResolvers;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Common.Tests.RequestCustomerResolvers;

[TestClass]
public class GuestCustomerPersistenceTests
{
    private Mock<ICustomerService> _customers;
    private Mock<IGroupService> _groups;
    private GuestCustomerPersistence _persistence;

    [TestInitialize]
    public void Setup()
    {
        _customers = new Mock<ICustomerService>();
        _customers.Setup(c => c.InsertGuestCustomer(It.IsAny<Customer>())).ReturnsAsync((Customer c) => c);
        _groups = new Mock<IGroupService>();
        _persistence = new GuestCustomerPersistence(_customers.Object, _groups.Object,
            NullLogger<GuestCustomerPersistence>.Instance);
    }

    [TestMethod]
    public async Task IsTransient_OnlyForTheTransientId()
    {
        Assert.IsFalse(_persistence.IsTransient("g1"));

        await _persistence.SetTransient(new Customer { Id = "g1" });

        Assert.IsTrue(_persistence.IsTransient("g1"));
        Assert.IsFalse(_persistence.IsTransient("other"));
        Assert.IsFalse(_persistence.IsTransient(null));
        Assert.IsFalse(_persistence.IsTransient(""));
    }

    [TestMethod]
    public async Task SetTransient_AddsGuestsGroup()
    {
        _groups.Setup(g => g.GetCustomerGroupBySystemName(SystemCustomerGroupNames.Guests))
            .ReturnsAsync(new CustomerGroup { Id = "guests" });
        var fresh = new Customer { Id = "g1" };
        var already = new Customer { Id = "g2" };
        already.Groups.Add("guests");

        await _persistence.SetTransient(fresh);
        await _persistence.SetTransient(already);

        CollectionAssert.AreEqual(new[] { "guests" }, fresh.Groups.ToArray());
        CollectionAssert.AreEqual(new[] { "guests" }, already.Groups.ToArray());
    }

    [TestMethod]
    public async Task EnsurePersisted_InsertsOnceAndClearsTransient()
    {
        var guest = new Customer { Id = "g1" };
        await _persistence.SetTransient(guest);

        await _persistence.EnsurePersisted();
        await _persistence.EnsurePersisted();

        _customers.Verify(c => c.InsertGuestCustomer(guest), Times.Once);
        Assert.IsNull(_persistence.Transient);
        Assert.IsFalse(_persistence.IsTransient("g1"));
    }

    [TestMethod]
    public async Task EnsurePersisted_NoTransient_DoesNothing()
    {
        await _persistence.EnsurePersisted();

        _customers.Verify(c => c.InsertGuestCustomer(It.IsAny<Customer>()), Times.Never);
    }

    [TestMethod]
    public async Task EnsurePersisted_ConcurrentCalls_InsertOnce()
    {
        var gate = new TaskCompletionSource<Customer>();
        _customers.Setup(c => c.InsertGuestCustomer(It.IsAny<Customer>())).Returns(gate.Task);
        await _persistence.SetTransient(new Customer { Id = "g1" });

        var first = _persistence.EnsurePersisted();
        var second = _persistence.EnsurePersisted();
        gate.SetResult(new Customer { Id = "g1" });
        await Task.WhenAll(first, second);

        _customers.Verify(c => c.InsertGuestCustomer(It.IsAny<Customer>()), Times.Once);
    }

    [TestMethod]
    public async Task EnsurePersisted_AfterFailedInsert_RetriesOnTheNextWrite()
    {
        var guest = new Customer { Id = "g1" };
        _customers.SetupSequence(c => c.InsertGuestCustomer(guest))
            .ThrowsAsync(new TimeoutException("db blip"))
            .ReturnsAsync(guest);
        await _persistence.SetTransient(guest);

        await Assert.ThrowsExactlyAsync<TimeoutException>(() => _persistence.EnsurePersisted());
        Assert.IsTrue(_persistence.IsTransient("g1"));

        await _persistence.EnsurePersisted();

        _customers.Verify(c => c.InsertGuestCustomer(guest), Times.Exactly(2));
        Assert.IsNull(_persistence.Transient);
    }

    [TestMethod]
    public async Task Promote_Inserts()
    {
        var guest = new Customer { Id = "g1", CustomerGuid = Guid.NewGuid() };

        Assert.AreSame(guest, await _persistence.Promote(guest));
    }

    [TestMethod]
    public async Task Promote_ParallelRequestWon_ReturnsTheExistingGuest()
    {
        //the unique CustomerGuid index rejected the second insert of the same cookie
        var guid = Guid.NewGuid();
        var existing = new Customer { Id = "first", CustomerGuid = guid, Active = true };
        _customers.Setup(c => c.InsertGuestCustomer(It.IsAny<Customer>()))
            .ThrowsAsync(new InvalidOperationException("duplicate key"));
        _customers.Setup(c => c.GetCustomerByGuid(guid)).ReturnsAsync(existing);

        var result = await _persistence.Promote(new Customer { Id = "second", CustomerGuid = guid });

        Assert.AreSame(existing, result);
    }

    [TestMethod]
    public async Task Promote_GuidTurnedOutToBeRegistered_ReturnsNull()
    {
        var guid = Guid.NewGuid();
        var admin = new Customer { Id = "admin", CustomerGuid = guid, Active = true };
        _customers.Setup(c => c.InsertGuestCustomer(It.IsAny<Customer>()))
            .ThrowsAsync(new InvalidOperationException("duplicate key"));
        _customers.Setup(c => c.GetCustomerByGuid(guid)).ReturnsAsync(admin);
        _groups.Setup(g => g.IsRegistered(admin)).ReturnsAsync(true);

        Assert.IsNull(await _persistence.Promote(new Customer { Id = "x", CustomerGuid = guid }));
    }

    [TestMethod]
    public async Task Promote_GuidTurnedOutToBeSystemAccount_ReturnsNull()
    {
        var guid = Guid.NewGuid();
        _customers.Setup(c => c.InsertGuestCustomer(It.IsAny<Customer>()))
            .ThrowsAsync(new InvalidOperationException("duplicate key"));
        _customers.Setup(c => c.GetCustomerByGuid(guid)).ReturnsAsync(
            new Customer { Id = "searchengine", CustomerGuid = guid, Active = true, IsSystemAccount = true });

        Assert.IsNull(await _persistence.Promote(new Customer { Id = "x", CustomerGuid = guid }));
    }

    [TestMethod]
    public async Task Promote_InsertFailsForAnotherReason_Throws()
    {
        _customers.Setup(c => c.InsertGuestCustomer(It.IsAny<Customer>()))
            .ThrowsAsync(new InvalidOperationException("db down"));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            _persistence.Promote(new Customer { Id = "x", CustomerGuid = Guid.NewGuid() }));
    }
}
