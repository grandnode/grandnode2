using Microsoft.VisualStudio.TestTools.UnitTesting;
using Grand.Business.Catalog.Queries.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grand.Domain.Data;
using Grand.Domain.Vendors;
using Grand.Data.Tests.MongoDb;

namespace Grand.Business.Catalog.Queries.Handlers.Tests
{
    [TestClass()]
    public class GetVendorByIdQueryHandlerTests
    {
        private IRepository<Vendor> _repository;
        private GetVendorByIdQueryHandler handler;
        [TestInitialize()]
        public void Init()
        {
            _repository = new MongoDBRepositoryTest<Vendor>();
            handler = new GetVendorByIdQueryHandler(_repository);
        }

        [TestMethod()]
        public async Task HandleTest()
        {
            //Arrange
            await _repository.InsertAsync(new Vendor() { Id = "1" });
            //Act
            var result = await handler.Handle(new Core.Queries.Catalog.GetVendorByIdQuery() { Id = "1" }, CancellationToken.None);
            //Arrange
            Assert.IsNotNull(result);
        }
    }
}