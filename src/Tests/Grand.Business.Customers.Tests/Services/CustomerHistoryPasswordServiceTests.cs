using Grand.Business.Customers.Services;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Customers.Tests.Services
{
    [TestClass()]
    public class CustomerHistoryPasswordServiceTests
    {
        private IRepository<CustomerHistoryPassword> _repository;
        private Mock<IMediator> _mediatorMock;
        private CustomerHistoryPasswordService _customerHistoryPasswordService;
        [TestInitialize()]
        public void Init()
        {
            _repository = new MongoDBRepositoryTest<CustomerHistoryPassword>();
            _mediatorMock = new Mock<IMediator>();
            _customerHistoryPasswordService = new CustomerHistoryPasswordService(_repository, _mediatorMock.Object);
        }


        [TestMethod()]
        public async Task InsertCustomerPasswordTest()
        {
            //Act
            await _customerHistoryPasswordService.InsertCustomerPassword(new Customer());
            //Asser
            Assert.IsTrue(_repository.Table.Count() > 0);
        }

        [TestMethod()]
        public async Task GetPasswordsTest()
        {
            //Arrange
            //Act
            await _customerHistoryPasswordService.InsertCustomerPassword(new Customer() {Id = "1" });
            await _customerHistoryPasswordService.InsertCustomerPassword(new Customer() { Id = "1" });

            //Act
            var result = await _customerHistoryPasswordService.GetPasswords("1", 1);
            //Asser
            Assert.IsTrue(result.Count() == 1);
        }
    }
}