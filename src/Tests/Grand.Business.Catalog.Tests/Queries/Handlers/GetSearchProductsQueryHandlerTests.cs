using Grand.Business.Catalog.Queries.Handlers;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Queries.Catalog;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Infrastructure.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Queries.Handlers;

[TestClass]
public class GetSearchProductsQueryHandlerTests
{
    private IRepository<Product> _repository;
    private GetSearchProductsQueryHandler handler;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<Product>();
        handler = new GetSearchProductsQueryHandler(_repository, new Mock<ISpecificationAttributeService>().Object,
            new CatalogSettings { IgnoreFilterableSpecAttributeOption = true }, new AccessControlConfig());
    }


    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        await _repository.InsertAsync(new Product { Published = true, VisibleIndividually = true });
        var searchProductsQuery = new GetSearchProductsQuery {
            Customer = new Customer()
        };
        //Act
        var result = await handler.Handle(searchProductsQuery, CancellationToken.None);
        //Arrange
        Assert.IsNotNull(result.products);
    }

    [TestMethod]
    public async Task Handle_WhenCustomerGroupIdsGivenWithoutCustomer_FiltersByThoseGroups()
    {
        //Arrange
        await _repository.InsertAsync(new Product { Id = "open", Published = true, VisibleIndividually = true });
        await _repository.InsertAsync(new Product {
            Id = "guests", Published = true, VisibleIndividually = true, LimitedToGroups = true,
            CustomerGroups = ["guests-group"]
        });
        await _repository.InsertAsync(new Product {
            Id = "admins", Published = true, VisibleIndividually = true, LimitedToGroups = true,
            CustomerGroups = ["admins-group"]
        });
        var searchProductsQuery = new GetSearchProductsQuery {
            CustomerGroupIds = ["guests-group"]
        };
        //Act
        var result = await handler.Handle(searchProductsQuery, CancellationToken.None);
        //Assert
        CollectionAssert.AreEquivalent(new[] { "open", "guests" }, result.products.Select(p => p.Id).ToList());
    }
}