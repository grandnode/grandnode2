﻿using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Data;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Tests.Caching;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Services.Products.Tests
{
    [TestClass()]
    public class RecentlyViewedProductsServiceTests
    {
        private IRepository<RecentlyViewedProduct> _repository;
        private MemoryCacheBase _cacheBase;
        private Mock<IMediator> _mediatorMock;
        private Mock<IProductService> _productServiceMock;
        RecentlyViewedProductsService _service;

        [TestInitialize()]
        public void Init()
        {
            _repository = new MongoDBRepositoryTest<RecentlyViewedProduct>();
            _mediatorMock = new Mock<IMediator>();
            _productServiceMock = new Mock<IProductService>();
            _productServiceMock.Setup(a => a.GetProductsByIds(It.IsAny<string[]>(), false)).Returns(() => Task.FromResult((IList<Product>)new List<Product>() { new Product() { Id = "1", Published = true }, new Product() { Id = "2", Published = true }, new Product() { Id = "3", Published = true } }));
            _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object);
            _service = new RecentlyViewedProductsService(_productServiceMock.Object, _cacheBase, new CatalogSettings() { RecentlyViewedProductsEnabled = true, RecentlyViewedProductsNumber = 10 }, _repository);
        }


        [TestMethod()]
        public async Task GetRecentlyViewedProductsTest()
        {
            //Arrange
            await _service.AddProductToRecentlyViewedList("1", "1");
            await _service.AddProductToRecentlyViewedList("1", "2");
            await _service.AddProductToRecentlyViewedList("1", "3");
            await _service.AddProductToRecentlyViewedList("2", "1");

            //Act
            var result = await _service.GetRecentlyViewedProducts("1", 10);

            //Assert
            Assert.AreEqual(3, result.Count);
        }

        [TestMethod()]
        public async Task AddProductToRecentlyViewedListTest()
        {
            //Act
            await _service.AddProductToRecentlyViewedList("1", "1");
            await _service.AddProductToRecentlyViewedList("1", "2");
            await _service.AddProductToRecentlyViewedList("1", "3");
            await _service.AddProductToRecentlyViewedList("2", "1");


            //Assert
            Assert.AreEqual(4, _repository.Table.Count());
        }
    }
}