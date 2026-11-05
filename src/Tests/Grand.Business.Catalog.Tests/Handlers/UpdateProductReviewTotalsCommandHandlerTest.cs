using Grand.Business.Catalog.Commands;
using Grand.Business.Core.Commands.Catalog;
using Grand.Business.Core.Events.Catalog;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using Moq;

namespace Grand.Business.Catalog.Tests.Handlers;

[TestClass]
public class UpdateProductReviewTotalsCommandHandlerTest
{
    private Mock<IMediator> _mediatorMock;
    private Mock<IMongoCollection<Product>> _mongoCollectionMock;
    private IRepository<Product> _productRepository;
    private Mock<IProductReviewService> _productReviewServiceMock;
    private UpdateProductReviewTotalsCommandHandler _updateProductReviewTotalsCommandHandler;


    [TestInitialize]
    public void Init()
    {
        var reviews = new List<ProductReview> {
            new() { Id = "1", ReplyText = "text1" },
            new() { Id = "2", ReplyText = "text2" }
        };

        _mongoCollectionMock = new Mock<IMongoCollection<Product>>();
        _mongoCollectionMock.SetupAllProperties();
        _mongoCollectionMock.Setup(x => x.UpdateOneAsync(It.IsAny<FilterDefinition<Product>>(),
            It.IsAny<UpdateDefinition<Product>>(),
            It.IsAny<UpdateOptions>(),
            default));

        _productRepository = new MongoDBRepositoryTest<Product>();
        _mediatorMock = new Mock<IMediator>();

        _productReviewServiceMock = new Mock<IProductReviewService>();
        IPagedList<ProductReview> pagedListReviews = new PagedList<ProductReview>(reviews, 0, 234567);
        _productReviewServiceMock.Setup(x => x.GetAllProductReviews(null,
            null,
            null,
            null,
            null,
            null,
            It.IsAny<string>(), 0, It.IsAny<int>())).Returns(Task.FromResult(pagedListReviews));

        _updateProductReviewTotalsCommandHandler = new UpdateProductReviewTotalsCommandHandler(
            _productRepository,
            _productReviewServiceMock.Object,
            _mediatorMock.Object);
    }

    [TestMethod]
    public async Task InsertProduct_NullArgument_ThrowException()
    {
        var request = new UpdateProductReviewTotalsCommand();

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(
            async () => await _updateProductReviewTotalsCommandHandler.Handle(request, default));
    }

    [TestMethod]
    public async Task UpdateProductReviews_ValidArgument_InvokeRepositoryAndPublish()
    {
        var request = new UpdateProductReviewTotalsCommand { Product = new Product() };
        await _updateProductReviewTotalsCommandHandler.Handle(request, default);

        _productReviewServiceMock.Verify(x => x.GetAllProductReviews(null, null, null, null, null,
            null, It.IsAny<string>(), 0, It.IsAny<int>()), Times.Once);
        _mediatorMock.Verify(x => x.Publish(It.Is<ProductChangedEvent>(e => e.ProductId == request.Product.Id && !e.ListsChanged),
            default), Times.Once);
    }
}