﻿using Grand.Business.Core.Interfaces.Common.Directory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Customers.Queries.Handlers.Tests
{
    [TestClass()]
    public class GetGroupBySystemNameQueryHandlerTests
    {
        private Mock<IGroupService> _groupServiceMock;
        private GetGroupBySystemNameQueryHandler handler;

        [TestInitialize()]
        public void Init()
        {
            _groupServiceMock = new Mock<IGroupService>();
            handler = new GetGroupBySystemNameQueryHandler(_groupServiceMock.Object);
        }

        [TestMethod()]
        public async Task HandleTest()
        {
            //Assert
            var groupBySystemNameQuery = new Core.Queries.Customers.GetGroupBySystemNameQuery();
            groupBySystemNameQuery.SystemName = "sample";

            //Act
            _ = await handler.Handle(groupBySystemNameQuery, CancellationToken.None);
            //Assert
            _groupServiceMock.Verify(c => c.GetCustomerGroupBySystemName(It.IsAny<string>()), Times.Once);
        }
    }
}