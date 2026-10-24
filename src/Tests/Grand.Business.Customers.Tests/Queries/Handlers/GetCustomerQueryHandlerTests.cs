using Grand.Business.Core.Queries.Customers;
using Grand.Business.Customers.Queries.Handlers;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Customers.Tests.Queries.Handlers;

[TestClass]
public class GetCustomerQueryHandlerTests
{
    private GetCustomerQueryHandler handler;

    [TestInitialize]
    public void Init()
    {
        var _repository = new MongoDBRepositoryTest<Customer>();
        _repository.Insert(new Customer());
        _repository.Insert(new Customer());
        _repository.Insert(new Customer());
        _repository.Insert(new Customer());
        handler = new GetCustomerQueryHandler(_repository);
    }

    [TestMethod]
    public async Task HandleTest()
    {
        //Assert
        var customerQuery = new GetCustomerQuery();
        //Act
        var result = await handler.Handle(customerQuery, CancellationToken.None);

        //Assert
        Assert.AreEqual(4, result.TotalCount);
    }

    [TestMethod]
    public async Task Handle_PageSizeTwo_ReturnsFirstPageAndTotal()
    {
        //Act
        var result = await handler.Handle(new GetCustomerQuery { PageIndex = 0, PageSize = 2 },
            CancellationToken.None);

        //Assert
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual(4, result.TotalCount);
        Assert.AreEqual(2, result.TotalPages);
    }

    [TestMethod]
    public async Task Handle_ExcludedGroupsAndStaffAccounts_LeftOutInTheQuery()
    {
        var repository = new MongoDBRepositoryTest<Customer>();
        var customer = new Customer { Email = "customer@x.com", Groups = { "registered" } };
        var wholesale = new Customer { Email = "wholesale@x.com", Groups = { "registered", "wholesale" } };
        repository.Insert(customer);
        repository.Insert(wholesale);
        repository.Insert(new Customer { Email = "vendor-group@x.com", Groups = { "registered", "vendors" } });
        repository.Insert(new Customer { Email = "manager-group@x.com", Groups = { "registered", "storemanager" } });
        repository.Insert(new Customer { Email = "vendor-link@x.com", Groups = { "registered" }, VendorId = "v1" });
        repository.Insert(new Customer { Email = "staff-link@x.com", Groups = { "registered" }, StaffStoreId = "s1" });
        var queryHandler = new GetCustomerQueryHandler(repository);

        var result = await queryHandler.Handle(new GetCustomerQuery {
            ExcludedCustomerGroupIds = ["vendors", "storemanager"],
            ExcludeVendorAndStaffAccounts = true
        }, CancellationToken.None);

        CollectionAssert.AreEquivalent(new[] { "customer@x.com", "wholesale@x.com" },
            result.Select(c => c.Email).ToArray());
        Assert.AreEqual(2, result.TotalCount);
    }

    [TestMethod]
    public async Task Handle_NoExclusions_KeepsEveryone()
    {
        var repository = new MongoDBRepositoryTest<Customer>();
        repository.Insert(new Customer { Groups = { "vendors" }, VendorId = "v1" });
        repository.Insert(new Customer { StaffStoreId = "s1" });

        var result = await new GetCustomerQueryHandler(repository).Handle(new GetCustomerQuery(),
            CancellationToken.None);

        Assert.AreEqual(2, result.TotalCount);
    }
}