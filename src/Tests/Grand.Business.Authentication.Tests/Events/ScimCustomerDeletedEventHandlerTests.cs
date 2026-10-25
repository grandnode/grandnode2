using Grand.Business.Authentication.Events;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Grand.Infrastructure.Events;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Authentication.Tests.Events;

[TestClass]
public class ScimCustomerDeletedEventHandlerTests
{
    private IRepository<ScimUser> _users;
    private IRepository<ScimGroup> _groups;
    private ScimCustomerDeletedEventHandler _handler;

    [TestInitialize]
    public void Init()
    {
        _users = new MongoDBRepositoryTest<ScimUser>();
        _groups = new MongoDBRepositoryTest<ScimGroup>();
        _handler = new ScimCustomerDeletedEventHandler(_users, _groups);
    }

    //a customer deleted in the admin panel must not leave a SCIM record that blocks provisioning the user again
    [TestMethod]
    public async Task CustomerDeleted_RemovesScimUserAndMemberships()
    {
        await _users.InsertAsync(new ScimUser { Id = "c1", ExternalId = "oid-1" });
        await _users.InsertAsync(new ScimUser { Id = "c2", ExternalId = "oid-2" });
        await _groups.InsertAsync(new ScimGroup { DisplayName = "Shop admins", Members = ["c1", "c2"] });

        await _handler.Handle(new EntityDeleted<Customer>(new Customer { Id = "c1" }), CancellationToken.None);

        Assert.IsNull(await _users.GetByIdAsync("c1"));
        Assert.IsNotNull(await _users.GetByIdAsync("c2"));
        CollectionAssert.AreEqual(new[] { "c2" }, _groups.Table.Single().Members.ToList());
    }
}
