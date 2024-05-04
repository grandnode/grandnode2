using Grand.Domain.Common;
using Grand.Domain.Customers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Domain.Tests.Common;

[TestClass]
public class UserFieldExtensionsTests
{
    private readonly Customer entity;

    public UserFieldExtensionsTests()
    {
        entity = new Customer();
        entity.UserFields.Add(new UserField { Key = "FirstName", Value = "Sara", StoreId = "" });
        entity.UserFields.Add(new UserField { Key = "LastName", Value = "Name", StoreId = "" });
        entity.UserFields.Add(new UserField { Key = "Registered", Value = "1", StoreId = "1" });
    }

    [TestMethod]
    public void GetUserFieldFromEntityTest_FirstName_NotNull()
    {
        var userField = entity.GetUserFieldFromEntity<string>("FirstName");
        Assert.IsNotNull(userField);
        Assert.AreEqual(userField, "Sara");
    }

    [TestMethod]
    public void GetUserFieldFromEntityTest_City_IsNull()
    {
        var userField = entity.GetUserFieldFromEntity<string>("City");
        Assert.IsNull(userField);
    }

    [TestMethod]
    public void GetUserFieldFromEntityTest_Registered_Store_NotNull()
    {
        var userField = entity.GetUserFieldFromEntity<string>("Registered", "1");
        Assert.IsNotNull(userField);
        Assert.AreEqual(userField, "1");
    }

    [TestMethod]
    public void GetUserFieldFromEntityTest_Registered_Store_Null()
    {
        var userField = entity.GetUserFieldFromEntity<string>("Registered", "2");
        Assert.IsNull(userField);
    }
}