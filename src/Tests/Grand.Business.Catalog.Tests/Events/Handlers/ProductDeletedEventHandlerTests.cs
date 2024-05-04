using Grand.Business.Catalog.Events.Handlers;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Seo;
using Grand.Infrastructure.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Events.Handlers;

[TestClass]
public class ProductDeletedEventHandlerTests
{
    private IRepository<CustomerGroupProduct> _customerGroupProductRepository;
    private IRepository<Customer> _customerRepository;
    private IRepository<EntityUrl> _entityUrlRepository;
    private ProductDeletedEventHandler _handler;
    private IRepository<ProductDeleted> _productDeletedRepository;
    private IRepository<ProductReview> _productReviewRepository;
    private IRepository<ProductTag> _productTagRepository;
    private Mock<IProductTagService> _productTagService;
    private IRepository<Product> _repository;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<Product>();
        _customerGroupProductRepository = new MongoDBRepositoryTest<CustomerGroupProduct>();
        _customerRepository = new MongoDBRepositoryTest<Customer>();
        _entityUrlRepository = new MongoDBRepositoryTest<EntityUrl>();
        _productTagRepository = new MongoDBRepositoryTest<ProductTag>();
        _productReviewRepository = new MongoDBRepositoryTest<ProductReview>();
        _productDeletedRepository = new MongoDBRepositoryTest<ProductDeleted>();
        _productTagService = new Mock<IProductTagService>();
        _handler = new ProductDeletedEventHandler(_repository, _customerGroupProductRepository,
            _customerRepository, _entityUrlRepository, _productTagRepository, _productReviewRepository,
            _productDeletedRepository, _productTagService.Object);
    }


    [TestMethod]
    public async Task HandleTest()
    {
        //Arrange
        var product = new Product();
        product.RecommendedProduct.Add("1");
        product.RelatedProducts.Add(new RelatedProduct { ProductId2 = "1" });
        product.CrossSellProduct.Add("1");
        product.SimilarProducts.Add(new SimilarProduct { ProductId2 = "1" });
        await _repository.InsertAsync(product);
        //Act
        await _handler.Handle(new EntityDeleted<Product>(new Product { Id = "1" }), CancellationToken.None);

        //Assert
        var result = _repository.Table.FirstOrDefault(x => x.Id == product.Id);
        Assert.IsNotNull(result);
        Assert.IsTrue(result.RelatedProducts.Count == 0);
        Assert.IsTrue(result.RecommendedProduct.Count == 0);
        Assert.IsTrue(result.CrossSellProduct.Count == 0);
        Assert.IsTrue(result.SimilarProducts.Count == 0);
    }
}