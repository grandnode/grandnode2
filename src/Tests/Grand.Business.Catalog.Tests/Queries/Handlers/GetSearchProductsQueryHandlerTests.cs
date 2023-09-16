using Grand.Business.Catalog.Queries.Handlers;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Data;
using Grand.Infrastructure.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Queries.Handlers
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
            handler = new GetSearchProductsQueryHandler(_repository, new Mock<ISpecificationAttributeService>().Object, new CatalogSettings() { IgnoreFilterableSpecAttributeOption = true}, new AccessControlConfig());
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