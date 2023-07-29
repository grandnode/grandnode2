using Grand.Business.Catalog.Services.Products;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Data;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Tests.Services.Products
{
    [TestClass()]
    public class ProductReviewServiceTests
    {
        private IRepository<ProductReview> _repository;
        private Mock<IMediator> _mediatorMock;
        private ProductReviewService _productReviewService;

        [TestInitialize()]
        public void Init()
        {
            _repository = new MongoDBRepositoryTest<ProductReview>();
            _mediatorMock = new Mock<IMediator>();
            _productReviewService = new ProductReviewService(_repository, _mediatorMock.Object);
        }


        [TestMethod()]
        public async Task GetAllProductReviewsTest()
        {
            //Arrange
            await _repository.InsertManyAsync(new[] {
            new ProductReview(){ CustomerId = "1" },
            new ProductReview(){ CustomerId = "1"},
            new ProductReview(){ },
            new ProductReview(){ }
            });

            //Act
            var result = await _productReviewService.GetAllProductReviews("1", null);

            //Assert
            Assert.AreEqual(2, result.Count());
        }

        [TestMethod()]
        public async Task InsertProductReviewTest()
        {
            //Act
            await _productReviewService.InsertProductReview(new ProductReview());
            //Assert
            Assert.IsTrue(_repository.Table.Any());
        }

        [TestMethod()]
        public async Task UpdateProductReviewTest()
        {
            //Arrange
            var productReview = new ProductReview() {
                ReviewText = "test"
            };
            await _productReviewService.InsertProductReview(productReview);
            productReview.ReviewText = "test2";

            //Act
            await _productReviewService.UpdateProductReview(productReview);

            //Assert
            Assert.IsNotNull(_repository.Table.FirstOrDefault(x => x.ReviewText == "test2"));
        }

        [TestMethod()]
        public async Task DeleteProductReviewTest()
        {
            //Arrange
            var productReview = new ProductReview() {
                ReviewText = "test"
            };
            await _productReviewService.InsertProductReview(productReview);
            //Act
            await _productReviewService.DeleteProductReview(productReview);

            //Assert
            Assert.IsNull(_repository.Table.FirstOrDefault(x => x.ReviewText == "test"));
            Assert.AreEqual(0, _repository.Table.Count());
        }

        [TestMethod()]
        public async Task GetProductReviewByIdTest()
        {
            //Arrange
            var productReview = new ProductReview() {
                ReviewText = "test"
            };
            await _productReviewService.InsertProductReview(productReview);

            //Act
            var result = await _productReviewService.GetProductReviewById(productReview.Id);

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("test", result.ReviewText);
        }
    }
}