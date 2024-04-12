using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Queries.Customers;
using Grand.Business.Customers.Queries.Handlers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Customers.Tests.Queries.Handlers;

[TestClass]
public class GetGroupBySystemNameQueryHandlerTests
{
    private Mock<IGroupService> _groupServiceMock;
    private GetGroupBySystemNameQueryHandler handler;

    [TestInitialize]
    public void Init()
    {
        _groupServiceMock = new Mock<IGroupService>();
        handler = new GetGroupBySystemNameQueryHandler(_groupServiceMock.Object);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Assert
        var groupBySystemNameQuery = new GetGroupBySystemNameQuery {
            SystemName = "sample"
        };

        //Act
        _ = await handler.Handle(groupBySystemNameQuery, CancellationToken.None);
        //Assert
        _groupServiceMock.Verify(c => c.GetCustomerGroupBySystemName(It.IsAny<string>()), Times.Once);
    }
}