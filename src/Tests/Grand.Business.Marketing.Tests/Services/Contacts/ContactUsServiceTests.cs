﻿using Grand.Data.Tests.MongoDb;
using Grand.Domain.Data;
using Grand.Domain.Messages;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;


namespace Grand.Business.Marketing.Services.Contacts.Tests
{
    [TestClass()]
    public class ContactUsServiceTests
    {
        private ContactUsService _contactUsService;
        private IRepository<ContactUs> _repository;
        private Mock<IMediator> _mediatorMock;

        [TestInitialize()]
        public void Init()
        {
            _repository = new MongoDBRepositoryTest<ContactUs>();
            _mediatorMock = new Mock<IMediator>();
            _contactUsService = new ContactUsService(_repository, _mediatorMock.Object);
        }

        [TestMethod()]
        public async Task DeleteContactUsTest()
        {
            //Arrange
            var contactUs = new ContactUs() {
                FullName = "test"
            };
            await _contactUsService.InsertContactUs(contactUs);

            //Act
            await _contactUsService.DeleteContactUs(contactUs);

            //Assert
            Assert.IsNull(_repository.Table.FirstOrDefault(x => x.FullName == "test"));
            Assert.AreEqual(0, _repository.Table.Count());
        }

        [TestMethod()]
        public async Task ClearTableTest()
        {
            //Arrange
            await _contactUsService.InsertContactUs(new ContactUs());
            await _contactUsService.InsertContactUs(new ContactUs());
            await _contactUsService.InsertContactUs(new ContactUs());

            //Act
            await _contactUsService.ClearTable();

            //Assert
            Assert.AreEqual(0, _repository.Table.Count());
        }

        [TestMethod()]
        public async Task GetAllContactUsTest()
        {
            //Arrange
            await _contactUsService.InsertContactUs(new ContactUs());
            await _contactUsService.InsertContactUs(new ContactUs());
            await _contactUsService.InsertContactUs(new ContactUs());

            //Act
            var result = await _contactUsService.GetAllContactUs();

            //Assert
            Assert.AreEqual(3, result.Count);
        }

        [TestMethod()]
        public async Task GetContactUsByIdTest()
        {
            //Arrange
            var contactUs = new ContactUs() {
                FullName = "test"
            };
            await _contactUsService.InsertContactUs(contactUs);

            //Act
            var result = await _contactUsService.GetContactUsById(contactUs.Id);

            //Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("test", result.FullName);
        }

        [TestMethod()]
        public async Task InsertContactUsTest()
        {
            //Act
            var contactUs = new ContactUs() {
                FullName = "test"
            };
            await _contactUsService.InsertContactUs(contactUs);

            //Assert
            Assert.IsTrue(_repository.Table.Any());
        }
    }
}