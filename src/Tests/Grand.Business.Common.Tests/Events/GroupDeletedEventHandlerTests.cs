using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Common.Events.Tests
{
    [TestClass()]
    public class GroupDeletedEventHandlerTests
    {

        private IRepository<Customer> _repository;
        private GroupDeletedEventHandler _handler;

        [TestInitialize()]
        public void Init()
        {
            _repository = new MongoDBRepositoryTest<Customer>();
            _handler = new GroupDeletedEventHandler(_repository);
        }

        [TestMethod()]
        public async Task HandleTest()
        {
            //Arrange
            var customer1 = new Customer();
            customer1.Groups.Add("1");
            customer1.Groups.Add("2");
            customer1.Groups.Add("3");
            await _repository.InsertAsync(customer1);
            var customer2 = new Customer();
            customer2.Groups.Add("1");
            customer2.Groups.Add("2");
            customer2.Groups.Add("3");
            await _repository.InsertAsync(customer2);
            //Act
            var notification = new Infrastructure.Events.EntityDeleted<CustomerGroup>(new CustomerGroup() { Id = "1" });
            await _handler.Handle(notification, CancellationToken.None);

            //Assert
            var count = _repository.Table.Where(x => x.Groups.Contains("1")).Count();
            Assert.AreEqual(0, count);
        }
    }
}