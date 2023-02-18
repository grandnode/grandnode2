using Microsoft.VisualStudio.TestTools.UnitTesting;
using Grand.Business.Catalog.Queries.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grand.Domain.Data;
using Grand.Domain.Catalog;
using Grand.Data.Tests.MongoDb;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Moq;

namespace Grand.Business.Catalog.Queries.Handlers.Tests
{
    [TestClass()]
    public class GetSearchProductsQueryHandlerTests
    {
        private IRepository<Product> _repository;
        private GetSearchProductsQueryHandler handler;
        [TestInitialize()]
        public void Init()
        {
            _repository = new MongoDBRepositoryTest<Product>();
            handler = new GetSearchProductsQueryHandler(_repository, new Mock<ISpecificationAttributeService>().Object, new CatalogSettings() { IgnoreFilterableSpecAttributeOption = true});
        }


        [TestMethod()]
        public async Task HandleTest()
        {
            //Arrange
            await _repository.InsertAsync(new Product() { Published = true, VisibleIndividually = true });
            var searchProductsQuery = new Core.Queries.Catalog.GetSearchProductsQuery();
            searchProductsQuery.Customer = new Domain.Customers.Customer();
            //Act
            var result = await handler.Handle(searchProductsQuery, CancellationToken.None);
            //Arrange
            Assert.IsNotNull(result);
        }
    }
}