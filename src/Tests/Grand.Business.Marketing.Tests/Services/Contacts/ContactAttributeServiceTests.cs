﻿using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Domain.Messages;
using Grand.Infrastructure;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Tests.Caching;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Marketing.Services.Contacts.Tests
{
    [TestClass()]
    public class ContactAttributeServiceTests
    {
        private ContactAttributeService _contactAttributeService;
        private IRepository<ContactAttribute> _repository;
        private MemoryCacheBase _cacheBase;
        private Mock<IMediator> _mediatorMock;
        private Mock<IWorkContext> _workContextMock;

        [TestInitialize()]
        public void Init()
        {
            _repository = new MongoDBRepositoryTest<ContactAttribute>();
            _mediatorMock = new Mock<IMediator>();
            _workContextMock = new Mock<IWorkContext>();
            _workContextMock.Setup(c => c.CurrentStore).Returns(() => new Domain.Stores.Store() { Id = "" });
            _workContextMock.Setup(c => c.CurrentCustomer).Returns(() => new Customer());

            _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object);
            _contactAttributeService = new ContactAttributeService(_cacheBase, _repository, _mediatorMock.Object, _workContextMock.Object);
        }

        [TestMethod()]
        public async Task DeleteContactAttributeTest()
        {
            //Arrange
            var contactAttribute = new ContactAttribute() {
                Name = "test"
            };
            await _contactAttributeService.InsertContactAttribute(contactAttribute);

            //Act
            await _contactAttributeService.DeleteContactAttribute(contactAttribute);

            //Assert
            Assert.IsNull(_repository.Table.FirstOrDefault(x => x.Name == "test"));
            Assert.AreEqual(0, _repository.Table.Count());
        }

        [TestMethod()]
        public async Task GetAllContactAttributesTest()
        {
            //Arrange
            await _contactAttributeService.InsertContactAttribute(new ContactAttribute());
            await _contactAttributeService.InsertContactAttribute(new ContactAttribute());
            await _contactAttributeService.InsertContactAttribute(new ContactAttribute());

            //Act
            var result = await _contactAttributeService.GetAllContactAttributes();

            //Assert
            Assert.AreEqual(3, result.Count);
        }

        [TestMethod()]
        public async Task GetContactAttributeByIdTest()
        {
            //Arrange
            var contactAttribute = new ContactAttribute() {
                Name = "test"
            };
            await _contactAttributeService.InsertContactAttribute(contactAttribute);

            //Act
            var result = await _contactAttributeService.GetContactAttributeById(contactAttribute.Id);

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("test", result.Name);
        }

        [TestMethod()]
        public async Task InsertContactAttributeTest()
        {
            //Act
            var contactAttribute = new ContactAttribute() {
                Name = "test"
            };
            await _contactAttributeService.InsertContactAttribute(contactAttribute);

            //Assert
            Assert.IsTrue(_repository.Table.Any());
        }

        [TestMethod()]
        public async Task UpdateContactAttributeTest()
        {
            //Arrange
            var contactAttribute = new ContactAttribute() {
                Name = "test"
            };
            await _contactAttributeService.InsertContactAttribute(contactAttribute);
            contactAttribute.Name = "test2";

            //Act
            await _contactAttributeService.UpdateContactAttribute(contactAttribute);

            //Assert
            Assert.IsNotNull(_repository.Table.FirstOrDefault(x => x.Name == "test2"));
        }
    }
}