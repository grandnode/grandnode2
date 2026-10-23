using Grand.Domain.Customers;
using Grand.Web.Admin.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class CustomerNoteControllerTests : CustomerControllerTestsBase<CustomerNoteController>
{
    protected override CustomerNoteController CreateController() => new(
        CustomerServiceMock.Object,
        CustomerNoteViewModelServiceMock.Object,
        ScopeMock.Object);

    [TestMethod]
    public async Task CustomerNoteDelete_CustomerOutOfScope_Throws_NothingDeleted()
    {
        var customer = new Customer { Id = "c1" };
        CustomerServiceMock.Setup(s => s.GetCustomerById("c1")).ReturnsAsync(customer);
        ScopeMock.Setup(s => s.HasAccess(customer)).ReturnsAsync(false);

        await Assert.ThrowsAsync<ArgumentException>(() => Controller.CustomerNoteDelete("n1", "c1"));
        CustomerNoteViewModelServiceMock.Verify(v => v.DeleteCustomerNote(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [TestMethod]
    public async Task CustomerNoteDelete_CustomerInScope_DeletesWithinThatCustomer()
    {
        SetupAuthorizedCustomer("c1");

        var result = await Controller.CustomerNoteDelete("n1", "c1");

        Assert.IsInstanceOfType(result, typeof(JsonResult));
        CustomerNoteViewModelServiceMock.Verify(v => v.DeleteCustomerNote("n1", "c1"), Times.Once);
    }

    [TestMethod]
    public async Task CustomerNoteAdd_CustomerOutOfScope_ReturnsFalse_NothingInserted()
    {
        CustomerServiceMock.Setup(s => s.GetCustomerById("missing")).ReturnsAsync((Customer)null);

        var result = await Controller.CustomerNoteAdd("missing", "", false, "title", "message");

        Assert.IsInstanceOfType(result, typeof(JsonResult));
        CustomerNoteViewModelServiceMock.Verify(v => v.InsertCustomerNote(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
