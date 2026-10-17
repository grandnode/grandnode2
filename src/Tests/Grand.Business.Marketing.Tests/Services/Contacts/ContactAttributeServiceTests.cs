using Grand.Business.Core.Utilities.Catalog;
using Grand.Business.Marketing.Services.Contacts;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Customers;
using Grand.Domain.Messages;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Configuration;
using Grand.Infrastructure.Tests.Caching;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Marketing.Tests.Services.Contacts;

[TestClass]
public class ContactAttributeServiceTests
{
    private MemoryCacheBase _cacheBase;
    private ContactAttributeService _contactAttributeService;
    private Mock<IMediator> _mediatorMock;
    private IRepository<ContactAttribute> _repository;

    [TestInitialize]
    public void Init()
    {
        _repository = new MongoDBRepositoryTest<ContactAttribute>();
        _mediatorMock = new Mock<IMediator>();
        var accessControlConfig = new AccessControlConfig();
        _cacheBase = new MemoryCacheBase(MemoryCacheTest.Get(), _mediatorMock.Object,
            new CacheConfig { DefaultCacheTimeMinutes = 1 });
        _contactAttributeService = new ContactAttributeService(_cacheBase, _repository, _mediatorMock.Object,
            accessControlConfig);
    }

    [TestMethod]
    public async Task DeleteContactAttributeTest()
    {
        //Arrange
        var contactAttribute = new ContactAttribute {
            Name = "test"
        };
        await _contactAttributeService.InsertContactAttribute(contactAttribute);

        //Act
        await _contactAttributeService.DeleteContactAttribute(contactAttribute);

        //Assert
        Assert.IsNull(_repository.Table.FirstOrDefault(x => x.Name == "test"));
        Assert.IsEmpty(_repository.Table);
    }

    [TestMethod]
    public async Task GetAllContactAttributesTest()
    {
        //Arrange
        await _contactAttributeService.InsertContactAttribute(new ContactAttribute());
        await _contactAttributeService.InsertContactAttribute(new ContactAttribute());
        await _contactAttributeService.InsertContactAttribute(new ContactAttribute());

        //Act
        var result = await _contactAttributeService.GetAllContactAttributes(CatalogAccess.For(new Customer(), ""));

        //Assert
        Assert.HasCount(3, result);
    }

    [TestMethod]
    public async Task GetAllContactAttributes_CustomersInDifferentGroups_SeeTheirOwnAttributes()
    {
        //Arrange
        await _contactAttributeService.InsertContactAttribute(new ContactAttribute { Name = "public" });
        await _contactAttributeService.InsertContactAttribute(new ContactAttribute {
            Name = "vip", LimitedToGroups = true, CustomerGroups = ["vip"]
        });
        var vip = new Customer();
        vip.Groups.Add("vip");
        var guest = new Customer();
        guest.Groups.Add("guests");

        //Act
        var forVip = await _contactAttributeService.GetAllContactAttributes(CatalogAccess.For(vip, ""));
        var forGuest = await _contactAttributeService.GetAllContactAttributes(CatalogAccess.For(guest, ""));

        //Assert
        Assert.HasCount(2, forVip);
        Assert.HasCount(1, forGuest);
        Assert.AreEqual("public", forGuest[0].Name);
    }

    [TestMethod]
    public async Task GetAllContactAttributes_IncludingHidden_ReturnsGroupLimitedAttributes()
    {
        //Arrange
        await _contactAttributeService.InsertContactAttribute(new ContactAttribute {
            Name = "vip", LimitedToGroups = true, CustomerGroups = ["vip"]
        });

        //Act
        var result = await _contactAttributeService.GetAllContactAttributes(CatalogAccess.IncludingHidden());

        //Assert
        Assert.HasCount(1, result);
    }

    [TestMethod]
    public async Task GetAllContactAttributes_Store_LeavesOutAttributesOfOtherStores()
    {
        //Arrange
        await _contactAttributeService.InsertContactAttribute(new ContactAttribute {
            Name = "other", LimitedToStores = true, Stores = ["store2"]
        });
        await _contactAttributeService.InsertContactAttribute(new ContactAttribute { Name = "all" });

        //Act
        var result = await _contactAttributeService.GetAllContactAttributes(CatalogAccess.IncludingHidden("store1"));

        //Assert
        Assert.HasCount(1, result);
        Assert.AreEqual("all", result[0].Name);
    }

    [TestMethod]
    public async Task GetContactAttributeByIdTest()
    {
        //Arrange
        var contactAttribute = new ContactAttribute {
            Name = "test"
        };
        await _contactAttributeService.InsertContactAttribute(contactAttribute);

        //Act
        var result = await _contactAttributeService.GetContactAttributeById(contactAttribute.Id);

        //Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("test", result.Name);
    }

    [TestMethod]
    public async Task InsertContactAttributeTest()
    {
        //Act
        var contactAttribute = new ContactAttribute {
            Name = "test"
        };
        await _contactAttributeService.InsertContactAttribute(contactAttribute);

        //Assert
        Assert.IsNotEmpty(_repository.Table);
    }

    [TestMethod]
    public async Task UpdateContactAttributeTest()
    {
        //Arrange
        var contactAttribute = new ContactAttribute {
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