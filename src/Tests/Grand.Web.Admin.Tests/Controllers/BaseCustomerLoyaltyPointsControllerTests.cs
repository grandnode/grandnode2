using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class BaseCustomerLoyaltyPointsControllerTests
    : CustomerControllerTestsBase<BaseCustomerLoyaltyPointsControllerTests.TestCustomerLoyaltyPointsController>
{
    public class TestCustomerLoyaltyPointsController(
        ICustomerService customerService,
        ICustomerLoyaltyPointsViewModelService customerLoyaltyPointsViewModelService,
        IAdminDataScope<Customer> scope)
        : BaseCustomerLoyaltyPointsController(customerService, customerLoyaltyPointsViewModelService, scope);

    protected override TestCustomerLoyaltyPointsController CreateController() => new(
        CustomerServiceMock.Object,
        CustomerLoyaltyPointsViewModelServiceMock.Object,
        ScopeMock.Object);

    [TestMethod]
    public async Task LoyaltyPointsHistoryAdd_StoreScope_ForcesStaffStoreId_IgnoringSubmittedStoreId()
    {
        var customer = SetupAuthorizedCustomer("c1");
        ScopeMock.Setup(s => s.DefaultStoreId).Returns("store-1");
        CustomerLoyaltyPointsViewModelServiceMock
            .Setup(v => v.InsertLoyaltyPointsHistory(customer, "store-1", 10, "msg"))
            .ReturnsAsync(new LoyaltyPointsHistory());

        // Caller submits a different, attacker-controlled storeId — must be ignored under Store scope.
        await Controller.LoyaltyPointsHistoryAdd("c1", "attacker-store", 10, "msg");

        CustomerLoyaltyPointsViewModelServiceMock.Verify(v => v.InsertLoyaltyPointsHistory(customer, "store-1", 10, "msg"), Times.Once);
    }

    [TestMethod]
    public async Task LoyaltyPointsHistoryAdd_GlobalScope_UsesSubmittedStoreId()
    {
        var customer = SetupAuthorizedCustomer("c1");
        CustomerLoyaltyPointsViewModelServiceMock
            .Setup(v => v.InsertLoyaltyPointsHistory(customer, "admin-submitted-store", 10, "msg"))
            .ReturnsAsync(new LoyaltyPointsHistory());

        await Controller.LoyaltyPointsHistoryAdd("c1", "admin-submitted-store", 10, "msg");

        CustomerLoyaltyPointsViewModelServiceMock.Verify(v => v.InsertLoyaltyPointsHistory(customer, "admin-submitted-store", 10, "msg"), Times.Once);
    }

    [TestMethod]
    public async Task LoyaltyPointsHistorySelect_Denied_ThrowsArgumentException()
    {
        CustomerServiceMock.Setup(s => s.GetCustomerById("missing")).ReturnsAsync((Customer)null);

        await Assert.ThrowsAsync<ArgumentException>(() => Controller.LoyaltyPointsHistorySelect("missing"));
        CustomerLoyaltyPointsViewModelServiceMock.Verify(v => v.PrepareLoyaltyPointsHistoryModel(It.IsAny<string>()), Times.Never);
    }
}
