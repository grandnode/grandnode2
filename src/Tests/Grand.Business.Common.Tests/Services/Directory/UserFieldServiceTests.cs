using Grand.Business.Common.Services.Directory;
using Grand.Data;
using Grand.Data.Tests.MongoDb;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson.Serialization.Conventions;

namespace Grand.Business.Common.Tests.Services.Directory;

[TestClass]
public class UserFieldServiceTests
{
    private IRepository<UserFieldBaseEntity> _repository;
    private IRepository<Customer> _repositoryCustomer;
    private UserFieldService _userFieldService;

    [TestInitialize]
    public void Init()
    {
        //global set an equivalent of [BsonIgnoreExtraElements] for every Domain Model
        var cp = new ConventionPack {
            new IgnoreExtraElementsConvention(true)
        };
        ConventionRegistry.Register("ApplicationConventions", cp, t => true);

        _repository = new MongoDBRepositoryTest<UserFieldBaseEntity>();
        _repositoryCustomer = new MongoDBRepositoryTest<Customer>();
        _userFieldService = new UserFieldService(_repository);
    }

    [TestMethod]
    public async Task SaveFieldTest_Insert()
    {
        //Arrange
        var customer = new Customer();
        _repositoryCustomer.Insert(customer);
        //Act
        await _userFieldService.SaveField(customer, "Field", "Value");
        //Assert
        var userFields = _repositoryCustomer.Table.FirstOrDefault(x => x.Id == customer.Id).UserFields;
        Assert.IsNotNull(userFields.FirstOrDefault(x => x.Key == "Field"));
    }

    [TestMethod]
    public async Task SaveFieldTest_Update()
    {
        //Arrange
        var customer = new Customer();
        customer.UserFields.Add(new UserField { Key = "Field", Value = "empty", StoreId = "" });
        _repositoryCustomer.Insert(customer);
        //Act
        await _userFieldService.SaveField(customer, "Field", "Value");
        //Assert
        var userFields = _repositoryCustomer.Table.FirstOrDefault(x => x.Id == customer.Id).UserFields;
        Assert.IsNotNull(userFields.FirstOrDefault(x => x.Key == "Field"));
        Assert.AreEqual("Value", userFields.FirstOrDefault(x => x.Key == "Field").Value);
    }

    [TestMethod]
    public async Task SaveFieldTest_Delete()
    {
        //Arrange
        var customer = new Customer();
        customer.UserFields.Add(new UserField { Key = "Field", Value = "Value", StoreId = "" });
        _repositoryCustomer.Insert(customer);
        //Act
        await _userFieldService.SaveField(customer, "Field", string.Empty);
        //Assert
        var userFields = _repositoryCustomer.Table.FirstOrDefault(x => x.Id == customer.Id).UserFields;
        Assert.IsNull(userFields.FirstOrDefault(x => x.Key == "Field"));
    }
}