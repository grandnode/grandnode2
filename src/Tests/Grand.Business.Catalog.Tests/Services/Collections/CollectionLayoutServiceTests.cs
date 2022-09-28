﻿using Grand.Data.Tests.MongoDb;
using Grand.Domain.Catalog;
using Grand.Domain.Data;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Tests.Caching;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Catalog.Services.Collections.Tests
{
    [TestClass()]
    public class CollectionLayoutServiceTests
    {
        private IRepository<CollectionLayout> _repository;
        private MemoryCacheBase _cacheBase;
        private Mock<IMediator> _mediatorMock;
        private CollectionLayoutService _collectionLayoutService;

        [TestInitialize()]
        public void Init()
        {
            _repository = new MongoDBRepositoryTest<CollectionLayout>();
            _mediatorMock = new Mock<IMediator>();

            _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object);
            _collectionLayoutService = new CollectionLayoutService(_repository, _cacheBase, _mediatorMock.Object);
        }


        [TestMethod()]
        public async Task GetAllCollectionLayoutsTest()
        {
            //Arrange
            await _collectionLayoutService.InsertCollectionLayout(new CollectionLayout());
            await _collectionLayoutService.InsertCollectionLayout(new CollectionLayout());
            await _collectionLayoutService.InsertCollectionLayout(new CollectionLayout());

            //Act
            var layouts = await _collectionLayoutService.GetAllCollectionLayouts();

            //Assert
            Assert.AreEqual(3, layouts.Count);
        }

        [TestMethod()]
        public async Task GetCollectionLayoutByIdTest()
        {
            //Arrange
            var collectionLayout = new CollectionLayout() {
                Name = "test"
            };
            await _collectionLayoutService.InsertCollectionLayout(collectionLayout);

            //Act
            var layout = await _collectionLayoutService.GetCollectionLayoutById(collectionLayout.Id);

            //Assert
            Assert.IsNotNull(layout);
            Assert.AreEqual("test", layout.Name);
        }

        [TestMethod()]
        public async Task InsertCollectionLayoutTest()
        {
            //Act
            await _collectionLayoutService.InsertCollectionLayout(new CollectionLayout());
            //Assert
            Assert.IsTrue(_repository.Table.Any());
        }

        [TestMethod()]
        public async Task UpdateCollectionLayoutTest()
        {
            //Arrange
            var categoryLayout = new CollectionLayout() {
                Name = "test"
            };
            await _collectionLayoutService.InsertCollectionLayout(categoryLayout);
            categoryLayout.Name = "test2";

            //Act
            await _collectionLayoutService.UpdateCollectionLayout(categoryLayout);

            //Assert
            Assert.IsNotNull(_repository.Table.FirstOrDefault(x => x.Name == "test2"));
        }

        [TestMethod()]
        public async Task DeleteCollectionLayoutTest()
        {

            //Arrange
            var collectionLayout1 = new CollectionLayout() {
                Name = "test1"
            };
            await _collectionLayoutService.InsertCollectionLayout(collectionLayout1);
            var collectionLayout2 = new CollectionLayout() {
                Name = "test2"
            };
            await _collectionLayoutService.InsertCollectionLayout(collectionLayout2);

            //Act
            await _collectionLayoutService.DeleteCollectionLayout(collectionLayout1);

            //Assert
            Assert.IsNull(_repository.Table.FirstOrDefault(x => x.Name == "test1"));
            Assert.AreEqual(1, _repository.Table.Count());
        }
    }
}