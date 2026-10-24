using Grand.Business.Authentication.Services;
using Grand.Business.Authentication.Services.Scim;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Business.Core.Utilities.Customers;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Domain.Vendors;
using Grand.Infrastructure.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Linq.Expressions;

namespace Grand.Business.Authentication.Tests.Services.Scim;

/// <summary>
///     The SCIM services over real test repositories; the customer service is a mock that writes to the customer repository,
///     so the tests assert the stored account, not the calls
/// </summary>
public class ScimFixture
{
    public static readonly CustomerGroup Administrators = Group("g-admin", SystemCustomerGroupNames.Administrators);
    public static readonly CustomerGroup StoreManager = Group("g-sm", SystemCustomerGroupNames.StoreManager);
    public static readonly CustomerGroup Vendors = Group("g-vendors", SystemCustomerGroupNames.Vendors);
    public static readonly CustomerGroup SalesManager = Group("g-sales", SystemCustomerGroupNames.SalesManager);
    public static readonly CustomerGroup Registered = Group("g-registered", SystemCustomerGroupNames.Registered);
    public static readonly CustomerGroup Guests = Group("g-guests", SystemCustomerGroupNames.Guests);
    public static readonly CustomerGroup Custom = Group("g-custom", "", false);
    public static readonly CustomerGroup OtherCustom = Group("g-custom2", "", false);

    private static readonly CustomerGroup[] AllGroups =
        [Administrators, StoreManager, Vendors, SalesManager, Registered, Guests, Custom, OtherCustom];

    public IRepository<Customer> Customers { get; } = new MongoDBRepositoryTest<Customer>();
    public IRepository<ScimUser> Users { get; } = new MongoDBRepositoryTest<ScimUser>();
    public IRepository<ScimGroup> Groups { get; } = new MongoDBRepositoryTest<ScimGroup>();
    public IRepository<ExternalAuthentication> Links { get; } = new MongoDBRepositoryTest<ExternalAuthentication>();

    public Mock<ICustomerService> CustomerService { get; } = new();
    public Mock<ICustomerManagerService> CustomerManager { get; } = new();
    public Mock<IGroupService> GroupService { get; } = new();
    public Mock<IOidcCustomerResolver> Resolver { get; } = new();
    public Mock<IOidcLinkService> LinkService { get; } = new();
    public Mock<IOidcSessionService> Sessions { get; } = new();
    public Mock<IOidcTokenRevocationService> Revocation { get; } = new();
    public Mock<IStoreService> StoreService { get; } = new();
    public Mock<IVendorService> VendorService { get; } = new();

    public ScimConfig Config { get; } = new() { Token = new string('t', 32) };
    public OidcLoginConfig LoginConfig { get; } = new();
    public CustomerConfig CustomerConfig { get; } = new();

    public ScimFixture()
    {
        CustomerService.Setup(x => x.GetCustomerById(It.IsAny<string>()))
            .Returns((string id) => Customers.GetByIdAsync(id));
        CustomerService.Setup(x => x.GetCustomersByIds(It.IsAny<string[]>()))
            .ReturnsAsync((string[] ids) => (IList<Customer>)Customers.Table.Where(x => ids.Contains(x.Id)).ToList());
        CustomerService.Setup(x => x.GetCustomerByEmail(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string email, string _) => Customers.GetOneAsync(x => x.Email == email.ToLowerInvariant()));
        CustomerService.Setup(x => x.GetCustomerByUsername(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string name, string _) => Customers.GetOneAsync(x => x.Username == name.ToLowerInvariant()));
        CustomerService.Setup(x => x.InsertGuestCustomer(It.IsAny<Customer>()))
            .Returns((Customer c) =>
            {
                c.Groups.Add(Guests.Id);
                return Customers.InsertAsync(c);
            });
        CustomerService.Setup(x => x.InsertCustomerGroupInCustomer(It.IsAny<CustomerGroup>(), It.IsAny<string>()))
            .Returns((CustomerGroup g, string id) => Customers.AddToCollectionField(id, x => x.Groups, g.Id));
        CustomerService.Setup(x => x.DeleteCustomerGroupInCustomer(It.IsAny<CustomerGroup>(), It.IsAny<string>()))
            .Returns((CustomerGroup g, string id) => Customers.RemoveCollectionFieldItem(id, x => x.Groups, y => y == g.Id));
        CustomerService.Setup(x => x.UpdateCustomerField(It.IsAny<string>(),
                It.IsAny<Expression<Func<Customer, string>>>(), It.IsAny<string>()))
            .Returns((string id, Expression<Func<Customer, string>> e, string v) => Customers.UpdateField(id, e, v));
        CustomerService.Setup(x => x.UpdateCustomerField(It.IsAny<string>(),
                It.IsAny<Expression<Func<Customer, bool>>>(), It.IsAny<bool>()))
            .Returns((string id, Expression<Func<Customer, bool>> e, bool v) => Customers.UpdateField(id, e, v));

        CustomerService.Setup(x => x.UpdateUserField(It.IsAny<Customer>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(async (Customer c, string key, string value, string _) =>
            {
                var stored = await Customers.GetByIdAsync(c.Id);
                stored.UserFields = stored.UserFields.Where(x => x.Key != key).ToList();
                if (!string.IsNullOrEmpty(value))
                    stored.UserFields.Add(new UserField { Key = key, Value = value, StoreId = "" });
                await Customers.UpdateAsync(stored);
            });

        CustomerManager.Setup(x => x.RegisterCustomer(It.IsAny<RegistrationRequest>()))
            .Returns(async (RegistrationRequest r) =>
            {
                r.Customer.Email = r.Email;
                r.Customer.Username = r.Username;
                r.Customer.StoreId = r.StoreId;
                r.Customer.Password = "hash";
                r.Customer.Active = r.IsApproved;
                r.Customer.Groups.Remove(Guests.Id);
                r.Customer.Groups.Add(Registered.Id);
                await Customers.UpdateAsync(r.Customer);
            });

        GroupService.Setup(x => x.GetAllByIds(It.IsAny<string[]>()))
            .ReturnsAsync((string[] ids) => (IList<CustomerGroup>)AllGroups.Where(g => ids.Contains(g.Id)).ToList());
        GroupService.Setup(x => x.GetCustomerGroupById(It.IsAny<string>()))
            .ReturnsAsync((string id) => AllGroups.FirstOrDefault(g => g.Id == id));
        GroupService.Setup(x => x.GetCustomerGroupBySystemName(It.IsAny<string>()))
            .ReturnsAsync((string name) => AllGroups.FirstOrDefault(g => g.SystemName == name));
        GroupService.Setup(x => x.IsRegistered(It.IsAny<Customer>()))
            .ReturnsAsync((Customer c) => c.Groups.Contains(Registered.Id));

        Resolver.Setup(x => x.LinkIdentity(It.IsAny<Customer>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(async (Customer c, string sub, string email, string _) =>
            {
                await Links.InsertAsync(new ExternalAuthentication {
                    CustomerId = c.Id, ExternalIdentifier = sub, Email = email,
                    ProviderSystemName = OidcCustomerResolver.ProviderSystemName
                });
                return OidcLinkResult.Linked;
            });
        LinkService.Setup(x => x.Remove(It.IsAny<Customer>(), It.IsAny<string>()))
            .Returns(async (Customer c, string linkId) =>
            {
                var link = await Links.GetOneAsync(x => x.Id == linkId && x.CustomerId == c.Id);
                if (link == null) return false;
                await Links.DeleteAsync(link);
                return true;
            });

        StoreService.Setup(x => x.GetStoreById(It.IsAny<string>()))
            .ReturnsAsync((string id) => new Store { Id = id });
        VendorService.Setup(x => x.GetVendorById(It.IsAny<string>()))
            .ReturnsAsync((string id) => new Vendor { Id = id });
    }

    private static CustomerGroup Group(string id, string systemName, bool isSystem = true) =>
        new() { Id = id, SystemName = systemName, Name = id, IsSystem = isSystem, Active = true };

    public ScimRoleSync RoleSync() =>
        new(Users, Groups, CustomerService.Object, GroupService.Object, NullLogger<ScimRoleSync>.Instance);

    public ScimAccountLock AccountLock() =>
        new(CustomerService.Object, Links, Sessions.Object, Revocation.Object, NullLogger<ScimAccountLock>.Instance);

    public ScimUserService UserService() =>
        new(Users, Groups, Links, CustomerService.Object, CustomerManager.Object, GroupService.Object, Resolver.Object,
            LinkService.Object, RoleSync(), AccountLock(), Config, LoginConfig, CustomerConfig, NullLogger<ScimUserService>.Instance);

    public ScimGroupService GroupServiceUnderTest() =>
        new(Groups, Users, GroupService.Object, StoreService.Object, VendorService.Object, RoleSync(),
            NullLogger<ScimGroupService>.Instance);

    public async Task<Customer> AddCustomer(string email, params string[] groupIds)
    {
        var customer = new Customer {
            Email = email.ToLowerInvariant(), Username = email.ToLowerInvariant(), Active = true, StoreId = ""
        };
        customer.Groups.Add(Registered.Id);
        foreach (var id in groupIds)
            customer.Groups.Add(id);
        return await Customers.InsertAsync(customer);
    }

    public Task<Customer> Reload(string id) => Customers.GetByIdAsync(id);

    public static ScimUserData Data(string userName = "jan@example.com", string externalId = "oid-1", bool active = true,
        string email = null, string given = "Jan", string family = "Nowak") =>
        new(userName, externalId, email, given, family, active);
}
