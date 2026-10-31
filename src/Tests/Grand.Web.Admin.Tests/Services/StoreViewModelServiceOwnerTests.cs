using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Queries.Customers;
using Grand.Domain;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Mediator;
using Grand.Web.AdminShared.Models.Stores;
using Grand.Web.AdminShared.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Services;

[TestClass]
public class StoreViewModelServiceOwnerTests
{
    private Mock<IMediator> _mediator;
    private StoreViewModelService _service;

    [TestInitialize]
    public void Init()
    {
        var groupService = new Mock<IGroupService>();
        groupService.Setup(x => x.GetCustomerGroupBySystemName(SystemCustomerGroupNames.Administrators))
            .ReturnsAsync(new CustomerGroup { Id = "admins" });
        groupService.Setup(x => x.GetCustomerGroupBySystemName(SystemCustomerGroupNames.StoreManager))
            .ReturnsAsync(new CustomerGroup { Id = "managers" });

        var administrators = new List<Customer> {
            Staff("admin", "admins", email: "admin@x.example"),
            Staff("inactive-admin", "admins", active: false),
            Staff("deleted-admin", "admins", deleted: true)
        };
        var customerService = new Mock<ICustomerService>();
        customerService.Setup(x => x.GetAllCustomers(It.IsAny<DateTime?>(), It.IsAny<DateTime?>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.Is<string[]>(ids => ids.Length == 1 && ids[0] == "admins"),
                It.IsAny<string[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<ShoppingCartType?>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Customer, object>>>(), It.IsAny<string[]>(),
                It.IsAny<bool>()))
            .ReturnsAsync(new PagedList<Customer>(administrators, 0, int.MaxValue, administrators.Count));

        //the store's managers come from a query scoped to the store, so another store's never reach memory
        var managers = new List<Customer> { Staff("manager", "managers", staffStoreId: "s1", email: "manager@x.example") };
        _mediator = new Mock<IMediator>();
        _mediator.Setup(x => x.Send(
                It.Is<GetCustomerQuery>(q => q.StaffStoreId == "s1" && q.CustomerGroupIds.Single() == "managers"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedList<Customer>(managers, 0, int.MaxValue, managers.Count));

        _service = new StoreViewModelService(new Mock<ILanguageService>().Object, new Mock<IWarehouseService>().Object,
            new Mock<IStoreService>().Object, new Mock<ISettingService>().Object, new Mock<ICountryService>().Object,
            new Mock<ICurrencyService>().Object, customerService.Object, groupService.Object, _mediator.Object);
    }

    private static Customer Staff(string id, string groupId, string staffStoreId = null, bool active = true,
        bool deleted = false, string email = null)
    {
        var customer = new Customer {
            Id = id, StaffStoreId = staffStoreId, Active = active, Deleted = deleted, Email = email ?? $"{id}@x.example"
        };
        customer.Groups.Add(groupId);
        return customer;
    }

    [TestMethod]
    public async Task PrepareOwnerAccounts_ListsAdministratorsAndThisStoresManagers()
    {
        var model = new StoreModel { Id = "s1" };

        await _service.PrepareOwnerAccountsModel(model);

        CollectionAssert.AreEqual(new[] { "", "admin", "manager" },
            model.AvailableOwnerAccounts.Select(x => x.Value).ToArray());
        Assert.AreEqual("---", model.AvailableOwnerAccounts[0].Text);
        StringAssert.Contains(model.AvailableOwnerAccounts[1].Text, "admin@x.example");
    }

    [TestMethod]
    public async Task PrepareOwnerAccounts_NewStore_ListsAdministratorsOnly()
    {
        var model = new StoreModel();

        await _service.PrepareOwnerAccountsModel(model);

        CollectionAssert.AreEqual(new[] { "", "admin" }, model.AvailableOwnerAccounts.Select(x => x.Value).ToArray());
        _mediator.Verify(x => x.Send(It.IsAny<GetCustomerQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [TestMethod]
    public async Task IsOwnerCandidate_TrueForListedAccount()
    {
        Assert.IsTrue(await _service.IsOwnerCandidate("s1", "admin"));
        Assert.IsTrue(await _service.IsOwnerCandidate("s1", "manager"));
    }

    [TestMethod]
    public async Task IsOwnerCandidate_FalseForRegisteredCustomer()
    {
        Assert.IsFalse(await _service.IsOwnerCandidate("s1", "registered"));
    }

    [TestMethod]
    public async Task IsOwnerCandidate_FalseForManagerOfAnotherStore()
    {
        Assert.IsFalse(await _service.IsOwnerCandidate("s1", "other-manager"));
    }

    [TestMethod]
    public async Task IsOwnerCandidate_FalseForInactiveOrDeletedAccount()
    {
        Assert.IsFalse(await _service.IsOwnerCandidate("s1", "inactive-admin"));
        Assert.IsFalse(await _service.IsOwnerCandidate("s1", "deleted-admin"));
    }
}
