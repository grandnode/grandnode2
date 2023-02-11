﻿using Grand.Business.Common.Services.Security;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Domain.Pages;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Tests.Caching;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Cms.Services.Tests
{
    [TestClass()]
    public class PageServiceTests
    {
        private PageService _pageService;

        private IRepository<Page> _repository;
        private Mock<IWorkContext> _workContextMock;
        private Mock<IMediator> _mediatorMock;
        private MemoryCacheBase _cacheBase;
        private IAclService _aclService;

        [TestInitialize()]
        public void Init()
        {
            _repository = new MongoDBRepositoryTest<Page>();

            _mediatorMock = new Mock<IMediator>();
            _workContextMock = new Mock<IWorkContext>();

            _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object);
            
            _aclService = new AclService();

            _workContextMock.Setup(c => c.CurrentStore).Returns(() => new Domain.Stores.Store() { Id = "", Name = "test store" });
            _workContextMock.Setup(c => c.CurrentCustomer).Returns(() => new Customer());

            _pageService = new PageService(_repository, _workContextMock.Object, _aclService, _mediatorMock.Object, _cacheBase);
        }

        [TestMethod()]
        public async Task GetPageByIdTest()
        {
            //Arrange
            var page = new Page() { };
            await _repository.InsertAsync(page);
            //Act
            var result = await _pageService.GetPageById(page.Id);
            //Assert
            Assert.IsNotNull(result);
        }

        [TestMethod()]
        public async Task GetPageBySystemNameTest()
        {
            //Arrange
            var page = new Page() { SystemName = "test" };
            await _repository.InsertAsync(page);
            //Act
            var result = await _pageService.GetPageBySystemName(page.SystemName);
            //Assert
            Assert.IsNotNull(result);
        }

        [TestMethod()]
        public async Task GetAllPagesTest()
        {
            //Arrange
            var page = new Page() { SystemName = "test", Published = true };
            await _repository.InsertAsync(page);
            //Act
            var result = await _pageService.GetAllPages("");
            //Assert
            Assert.IsTrue(result.Any());
        }

        [TestMethod()]
        public async Task InsertPageTest()
        {
            //Arrange
            var page = new Page() { };
            //Act
            await _pageService.InsertPage(page);
            //Assert
            Assert.IsNotNull(_repository.Table.FirstOrDefault(x => x.Id == page.Id));
        }

        [TestMethod()]
        public async Task UpdatePageTest()
        {
            //Arrange
            var page = new Page() { };
            await _pageService.InsertPage(page);
            //Act
            page.SystemName = "test";
            await _pageService.UpdatePage(page);
            //Assert
            Assert.IsTrue(_repository.Table.FirstOrDefault(x => x.Id == page.Id).SystemName == "test");
        }

        [TestMethod()]
        public async Task DeletePageTest()
        {
            //Arrange
            var page = new Page() { };
            await _pageService.InsertPage(page);
            //Act
            await _pageService.DeletePage(page);
            //Assert
            Assert.IsNull(_repository.Table.FirstOrDefault(x => x.Id == page.Id));
        }
    }
}