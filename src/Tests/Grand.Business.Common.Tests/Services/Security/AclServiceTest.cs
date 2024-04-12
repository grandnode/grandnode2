using Grand.Business.Common.Services.Security;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Grand.Business.Common.Tests.Services.Security;

[TestClass]
public class AclServiceTest
{
    private AccessControlConfig _accessControlConfig;
    private AclService _aclService;
    private CatalogSettings _settings;

    [TestInitialize]
    public void Init()
    {
        _settings = new CatalogSettings();
        _accessControlConfig = new AccessControlConfig();
        _aclService = new AclService(_accessControlConfig);
    }

    [TestMethod]
    public void Authorize_ReturnFalse()
    {
        Product product = null;
        Assert.IsFalse(_aclService.Authorize(product, "id"));
        product = new Product {
            LimitedToStores = true
        };
        Assert.IsFalse(_aclService.Authorize(product, "id"));
    }

    [TestMethod]
    public void Authorize_ReturnTrue()
    {
        var product = new Product {
            LimitedToStores = false
        };
        Assert.IsTrue(_aclService.Authorize(product, "id"));
        Assert.IsTrue(_aclService.Authorize(product, ""));
        _accessControlConfig.IgnoreStoreLimitations = true;
        Assert.IsTrue(_aclService.Authorize(product, "id"));
    }
}