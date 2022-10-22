﻿using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Messages.Queries.Handlers.Tests
{

    [TestClass()]
    public class GetBidsByProductIdQueryHandlerTests
    {
        private GetBidsByProductIdQueryHandler handler;

        [TestInitialize()]
        public void Init()
        {
            var _repository = new MongoDBRepositoryTest<Bid>();
            _repository.Insert(new Bid() { Id = "1", ProductId = "1" });
            _repository.Insert(new Bid());
            _repository.Insert(new Bid());
            _repository.Insert(new Bid());

            handler = new GetBidsByProductIdQueryHandler(_repository);
        }

        [TestMethod()]
        public async Task HandleTest()
        {
            //Act
            var result = await handler.Handle(new Core.Queries.Messages.GetBidsByProductIdQuery() { ProductId = "1" }, CancellationToken.None);
            //Assert
            Assert.IsNotNull(result);
        }
    }
}