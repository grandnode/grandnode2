﻿using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Data;
using Grand.Domain.Seo;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Tests.Caching;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Events.Handlers.Tests
{
    [TestClass()]
    public class CategoryDeletedEventHandlerTests
    {
        private IRepository<Product> _repository;
        private IRepository<EntityUrl> _entityUrlRepository;
        private MemoryCacheBase _cacheBase;
        private Mock<IMediator> _mediatorMock;

        private CategoryDeletedEventHandler _handler;

        [TestInitialize()]
        public void Init()
        {
            _mediatorMock = new Mock<IMediator>();
            _repository = new MongoDBRepositoryTest<Product>();
            _entityUrlRepository = new MongoDBRepositoryTest<EntityUrl>();
            _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object);

            _handler = new CategoryDeletedEventHandler(_entityUrlRepository, _repository, _cacheBase);
        }


        [TestMethod()]
        public async Task HandleTest()
        {
            //Arrange
            var category = new Category();
            var product = new Product();
            product.ProductCategories.Add(new ProductCategory() { CategoryId = category.Id });
            await _repository.InsertAsync(product);
            var product2 = new Product();
            product2.ProductCategories.Add(new ProductCategory() { CategoryId = category.Id });
            await _repository.InsertAsync(product2);
            var product3 = new Product();
            product3.ProductCategories.Add(new ProductCategory() { CategoryId = "1" });
            await _repository.InsertAsync(product3);

            //Act
            await _handler.Handle(new Infrastructure.Events.EntityDeleted<Category>(category), CancellationToken.None);
            //Assert
            Assert.AreEqual(0, _repository.Table.Where(x => x.ProductCategories.Any(y => y.CategoryId == category.Id)).Count());
            Assert.AreEqual(1, _repository.Table.Where(x => x.ProductCategories.Any(y => y.CategoryId == "1")).Count());
        }
    }
}