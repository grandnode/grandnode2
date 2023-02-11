﻿using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Messages.Queries.Handlers.Tests
{
    [TestClass()]
    public class GetCustomerByIdQueryHandlerTests
    {
        private GetCustomerByIdQueryHandler handler;

        [TestInitialize()]
        public void Init()
        {
            var _repository = new MongoDBRepositoryTest<Customer>();
            _repository.Insert(new Customer() { Id = "1" });
            _repository.Insert(new Customer());
            _repository.Insert(new Customer());
            _repository.Insert(new Customer());
            handler = new GetCustomerByIdQueryHandler(_repository);
        }

        [TestMethod()]
        public async Task HandleTest()
        {
            //Assert
            var customerQuery = new Core.Queries.Messages.GetCustomerByIdQuery();
            customerQuery.Id = "1";
            //Act
            var result = await handler.Handle(customerQuery, CancellationToken.None);

            //Assert
            Assert.IsNotNull(result);
        }
    }
}