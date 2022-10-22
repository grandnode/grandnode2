﻿using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Customers.Queries.Handlers.Tests
{
    [TestClass()]
    public class GetPasswordIsExpiredQueryHandlerTests
    {
        private Mock<IGroupService> _groupServiceMock;
        private GetPasswordIsExpiredQueryHandler handler;

        [TestInitialize()]
        public void Init()
        {
            _groupServiceMock = new Mock<IGroupService>();
            handler = new GetPasswordIsExpiredQueryHandler(_groupServiceMock.Object, new Domain.Customers.CustomerSettings() { PasswordLifetime = 10 });
        }

        [TestMethod()]
        public async Task HandleTest()
        {
            //Assert
            var cr = new CustomerGroup() { Active = true, EnablePasswordLifetime = true };
            var cgs = new List<CustomerGroup>() { cr };
            _groupServiceMock.Setup(c => c.GetAllByIds(It.IsAny<string[]>()))
                .Returns(Task.FromResult<IList<CustomerGroup>>(cgs));

            var passwordIsExpiredQuery = new Core.Queries.Customers.GetPasswordIsExpiredQuery();
            var customer = new Customer() { Email = "email@email.com" };
            customer.Groups.Add(cr.Id);
            passwordIsExpiredQuery.Customer = customer;
            //Act
            var result = await handler.Handle(passwordIsExpiredQuery, CancellationToken.None);
            //Assert
            Assert.IsTrue(result);
        }
    }
}