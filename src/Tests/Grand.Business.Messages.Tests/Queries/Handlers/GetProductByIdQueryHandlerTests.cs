using Microsoft.VisualStudio.TestTools.UnitTesting;
using Grand.Business.Messages.Queries.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grand.Domain.Catalog;
using Grand.Domain.Data;
using Moq;
using Grand.Data.Tests.MongoDb;

namespace Grand.Business.Messages.Queries.Handlers.Tests
{
    [TestClass()]
    public class GetProductByIdQueryHandlerTests
    {
        private GetProductByIdQueryHandler handler;

        [TestInitialize()]
        public void Init()
        {
            var _repository = new MongoDBRepositoryTest<Product>();
            _repository.Insert(new Product() { Id = "1" });
            _repository.Insert(new Product());
            _repository.Insert(new Product());
            _repository.Insert(new Product());

            handler = new GetProductByIdQueryHandler(_repository);
        }

        [TestMethod()]
        public async Task HandleTest()
        {
            //Act
            var result = await handler.Handle(new Core.Queries.Messages.GetProductByIdQuery() { Id = "1" }, CancellationToken.None);
            //Assert
            Assert.IsNotNull(result);
        }
    }

}