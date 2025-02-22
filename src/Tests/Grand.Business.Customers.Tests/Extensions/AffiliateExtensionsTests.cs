﻿using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Affiliates;
using Grand.Domain.Common;
using Grand.Domain.Seo;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Customers.Tests.Extensions;

[TestClass]
public class AffiliateExtensionsTests
{
    private const string _fakeStoreUrl = "http://localhost/";
    private readonly string _expectedFormat = _fakeStoreUrl + "?{0}={1}";
    private Mock<IAffiliateService> _affiliateServiceMock;
    private Mock<IWorkContext> _workContextMock;
    private Mock<IStoreContext> _storeContextMock;

    [TestInitialize]
    public void TestInitialize()
    {
        _workContextMock = new Mock<IWorkContext>();
        _storeContextMock = new Mock<IStoreContext>();
        _affiliateServiceMock = new Mock<IAffiliateService>();
    }


    [TestMethod]
    public void GetFullName_NullParameter_ThrowException()
    {
        Affiliate affiliate = null;
        Assert.ThrowsException<ArgumentNullException>(() => affiliate.GetFullName());
    }

    [TestMethod]
    public void GetFullName_NullLastName_ReturnFirstName()
    {
        var firstName = "Test name";
        var affiliate = new Affiliate {
            Address = new Address { FirstName = firstName }
        };
        Assert.AreEqual(firstName, affiliate.GetFullName());
    }

    [TestMethod]
    public void GetFullName_NullFirstName_ReturnLastName()
    {
        var lastName = "Test name";
        var affiliate = new Affiliate {
            Address = new Address { LastName = lastName }
        };
        Assert.AreEqual(lastName, affiliate.GetFullName());
    }

    [TestMethod]
    public void GetFullName_ValidParameter_ReturnLastName()
    {
        var fullName = $"{"first name"} {"lastName"}";
        var affiliate = new Affiliate {
            Address = new Address { LastName = "lastName", FirstName = "first name" }
        };
        Assert.AreEqual(fullName, affiliate.GetFullName());
    }

    [TestMethod]
    public void GenerateUrl_NullAffiliate_ThrowException()
    {
        Affiliate affiliate = null;
        Assert.ThrowsException<ArgumentNullException>(() => affiliate.GenerateUrl(null), "affiliate");
    }

    [TestMethod]
    public void GenerateUrl_NullWebHelper_ThrowException()
    {
        var affiliate = new Affiliate();
        Assert.ThrowsException<ArgumentNullException>(() => affiliate.GenerateUrl(null), "webHelper");
    }

    [TestMethod]
    public void GenerateUrl_NullFriendlyUrlName_UseId()
    {
        var id = "id";
        var affiliate = new Affiliate {
            Id = id
        };
        _storeContextMock.Setup(c => c.CurrentStore).Returns(new Store { Url = _fakeStoreUrl });
        Assert.AreEqual(string.Format(_expectedFormat, "affiliateid", id), affiliate.GenerateUrl("http://localhost/"));
        //_webHelperMock.Verify(c => c.ModifyQueryString(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public void GenerateUrl_ValideParameters()
    {
        var friendlyUrl = "friendlyurl";
        var affiliate = new Affiliate {
            FriendlyUrlName = friendlyUrl
        };
        _storeContextMock.Setup(c => c.CurrentStore).Returns(new Store { Url = _fakeStoreUrl });
        Assert.AreEqual(string.Format(_expectedFormat, "affiliate", friendlyUrl),
            affiliate.GenerateUrl("http://localhost/"));
    }
}