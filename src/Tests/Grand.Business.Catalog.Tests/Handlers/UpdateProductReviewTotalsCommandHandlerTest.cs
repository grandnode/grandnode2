﻿//using Grand.Business.Catalog.Commands.Handlers;
//using Grand.Business.Core.Interfaces.Catalog.Products;
//using Grand.Domain;
//using Grand.Domain.Catalog;
//using Grand.Domain.Data.Mongo;
//using Grand.Infrastructure.Caching;
//using Grand.SharedKernel.Extensions;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using Moq;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace Grand.Business.Catalog.Tests.Handlers
//{
//    [TestClass()]
//    public class UpdateProductReviewTotalsCommandHandlerTest
//    {
//        private Mock<MongoRepository<Product>> _productRepositoryMock;
//        private Mock<ICacheBase> _cacheBaseMock;
//        private UpdateProductReviewTotalsCommandHandler _updateProductReviewTotalsCommandHandler;
//        private Mock<IMongoCollection<Product>> _mongoCollectionMock;
//        private Mock<IProductReviewService> _productReviewServiceMock;


//        [TestInitialize()]
//        public void Init()
//        {
//            CommonPath.BaseDirectory = "";

//            var reviews = new List<ProductReview> {
//                new ProductReview { Id = "1", ReplyText = "text1"},
//                new ProductReview { Id = "2", ReplyText = "text2"}};

//            _mongoCollectionMock = new Mock<IMongoCollection<Product>>();
//            _mongoCollectionMock.SetupAllProperties();
//            _mongoCollectionMock.Setup(x => x.UpdateOneAsync(It.IsAny<FilterDefinition<Product>>(),
//                It.IsAny<UpdateDefinition<Product>>(),
//                It.IsAny<UpdateOptions>(),
//                default));

//            _productRepositoryMock = new Mock<MongoRepository<Product>>();
//            _productRepositoryMock.Setup(x => x.Collection).Returns(_mongoCollectionMock.Object);

//            _cacheBaseMock = new Mock<ICacheBase>();

//            _productReviewServiceMock = new Mock<IProductReviewService>();
//            IPagedList<ProductReview> pagedListReviews = new PagedList<ProductReview>(reviews, 0, 234567);
//            _productReviewServiceMock.Setup(x => x.GetAllProductReviews(null,
//                null,
//                null,
//                null,
//                null,
//                null,
//                It.IsAny<string>(), 0, It.IsAny<int>())).Returns(Task.FromResult(pagedListReviews));

//            _updateProductReviewTotalsCommandHandler = new UpdateProductReviewTotalsCommandHandler(
//                _productRepositoryMock.Object,
//                _productReviewServiceMock.Object,
//                _cacheBaseMock.Object);
//        }

//        //[TestMethod()]
//        //public async Task InsertProduct_NullArgument_ThrowException()
//        //{
//        //    var request = new UpdateProductReviewTotalsCommand();

//        //    await Assert.ThrowsExceptionAsync<ArgumentNullException>(
//        //        async () => await _updateProductReviewTotalsCommandHandler.Handle(request, default));
//        //}

//        //[TestMethod()]
//        //public async Task UpdateProductReviews_ValidArgument_InvokeRepositoryAndCache()
//        //{
//        //    var request = new UpdateProductReviewTotalsCommand { Product = new Product() };
//        //    await _updateProductReviewTotalsCommandHandler.Handle(request, default);

//        //    //_productReviewServiceMock.Verify(x => x.GetAllProductReviews(null, null, null, null, null,
//        //    //    null, It.IsAny<string>(), 0, It.IsAny<int>()), Times.Once);
//        //    //TODO
//        //    //_productRepositoryMock.Verify(x => x.Collection.UpdateOneAsync(It.IsAny<FilterDefinition<Product>>(),
//        //    //    It.IsAny<UpdateDefinition<Product>>(),
//        //    //    It.IsAny<UpdateOptions>(),
//        //    //    default), Times.Once);
//        //}
//    }
//}
