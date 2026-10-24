using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Authentication;
using Grand.Domain;
using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Domain.Vendors;
using Grand.Infrastructure.Configuration;
using Grand.Web.Admin.Controllers;
using Grand.Web.AdminShared.Models.ExternalAuthentication;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Controllers;

[TestClass]
public class ExternalAuthenticationControllerScimTests
{
    private const string Token = "0123456789abcdef0123456789abcdef";

    private readonly Mock<IScimGroupService> _groups = new();
    private readonly Mock<IScimUserService> _users = new();
    private readonly Mock<IStoreService> _stores = new();
    private readonly Mock<IVendorService> _vendors = new();
    private readonly Mock<ICustomerService> _customers = new();
    private readonly Mock<IRequestTranslationService> _translation = new();
    private ExternalAuthenticationController _controller;

    [TestInitialize]
    public void Init()
    {
        _controller = new ExternalAuthenticationController(new Mock<IExternalAuthenticationService>().Object,
            new ExternalAuthenticationSettings(), new Mock<ISettingService>().Object, new Mock<IServiceProvider>().Object);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("shop.example.com");
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        _controller.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);

        _groups.Setup(x => x.GetMappableGroups()).ReturnsAsync([
            new CustomerGroup { Id = "g-admin", Name = "Administrators" }
        ]);
        _groups.Setup(x => x.GetMappings()).ReturnsAsync([
            new ScimGroupMapping("sg1", "Shop admins", 2, "g-admin", "", "")
        ]);
        _stores.Setup(x => x.GetAllStores()).ReturnsAsync([new Store { Id = "s1", Shortcut = "Main" }]);
        _vendors.Setup(x => x.GetAllVendors(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
            .ReturnsAsync(new PagedList<Vendor>([new Vendor { Id = "v1", Name = "Vendor" }], 0, int.MaxValue));
        _users.Setup(x => x.Count()).ReturnsAsync(5);
        _users.Setup(x => x.GetConflicts()).ReturnsAsync(["c1"]);
        _customers.Setup(x => x.GetCustomersByIds(It.IsAny<string[]>()))
            .ReturnsAsync([new Customer { Id = "c1", Email = "jan@example.com" }]);
        _translation.Setup(x => x.GetResource(It.IsAny<string>())).Returns((string key) => key);
    }

    private Task<IActionResult> Scim(string token = Token, bool moduleOn = true, string clientId = "shop") =>
        _controller.Scim(new ScimConfig { Token = token }, new OidcLoginConfig { ClientId = clientId },
            new OidcResourceServerConfig { Authority = "https://idp.example.com" },
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> {
                ["FeatureManagement:Grand.Module.Scim"] = moduleOn.ToString()
            }).Build(),
            _users.Object, _customers.Object);

    [TestMethod]
    public async Task Scim_ShowsStatusButNeverTheToken()
    {
        var model = (ScimModel)((ViewResult)await Scim()).Model!;

        Assert.IsTrue(model.ModuleEnabled);
        Assert.IsTrue(model.TokenUsable);
        Assert.IsTrue(model.SignInConfigured);
        Assert.AreEqual("https://shop.example.com/scim/v2", model.EndpointUrl);
        Assert.AreEqual(5, model.ManagedAccounts);
        Assert.AreEqual("jan@example.com", model.Conflicts.Single().Email);
        foreach (var property in typeof(ScimModel).GetProperties().Where(x => x.PropertyType == typeof(string)))
            Assert.IsFalse(((string)property.GetValue(model))?.Contains(Token) ?? false, property.Name);
    }

    [TestMethod]
    public async Task Scim_ModuleOffAndNoSignIn_Reported()
    {
        var model = (ScimModel)((ViewResult)await Scim(token: "short", moduleOn: false, clientId: "")).Model!;

        Assert.IsFalse(model.ModuleEnabled);
        Assert.IsFalse(model.TokenUsable);
        Assert.IsFalse(model.SignInConfigured);
    }

    [TestMethod]
    public async Task ScimGroupEdit_Unknown_RedirectsToScim()
    {
        _groups.Setup(x => x.Get("nope")).ReturnsAsync(new ScimGroupResult(ScimOutcome.NotFound));
        var result = await _controller.ScimGroupEdit("nope", _groups.Object, _stores.Object, _vendors.Object);
        Assert.AreEqual("Scim", ((RedirectToActionResult)result).ActionName);
    }

    [TestMethod]
    public async Task ScimGroupEdit_Get_OffersMappableGroupsStoresVendors()
    {
        _groups.Setup(x => x.Get("sg1")).ReturnsAsync(new ScimGroupResult(ScimOutcome.Ok,
            new ScimGroupView("sg1", "Shop admins", null, ["c1"], DateTime.UtcNow, null)));

        var model = (ScimGroupMappingModel)((ViewResult)await _controller.ScimGroupEdit("sg1", _groups.Object,
            _stores.Object, _vendors.Object)).Model!;

        Assert.AreEqual("Shop admins", model.DisplayName);
        Assert.AreEqual("g-admin", model.CustomerGroupId);
        Assert.IsTrue(model.AvailableCustomerGroups.Any(x => x.Value == "g-admin"));
        Assert.IsTrue(model.AvailableStores.Any(x => x.Value == "s1"));
        Assert.IsTrue(model.AvailableVendors.Any(x => x.Value == "v1"));
    }

    [TestMethod]
    public async Task ScimGroupEdit_Post_InvalidTarget_StaysWithError()
    {
        _groups.Setup(x => x.SetMapping("sg1", "g-sm", "", "")).ReturnsAsync(ScimOutcome.Invalid);
        _groups.Setup(x => x.Get("sg1")).ReturnsAsync(new ScimGroupResult(ScimOutcome.Ok,
            new ScimGroupView("sg1", "Shop admins", null, [], DateTime.UtcNow, null)));

        var result = await _controller.ScimGroupEdit(new ScimGroupMappingModel {
            Id = "sg1", CustomerGroupId = "g-sm", StaffStoreId = "", VendorId = ""
        }, _groups.Object, _stores.Object, _vendors.Object, _translation.Object);

        Assert.IsInstanceOfType<ViewResult>(result);
        Assert.IsFalse(_controller.ModelState.IsValid);
    }

    [TestMethod]
    public async Task ScimGroupEdit_Post_Valid_SavesAndRedirects()
    {
        _groups.Setup(x => x.SetMapping("sg1", "g-admin", null, null)).ReturnsAsync(ScimOutcome.Ok);

        var result = await _controller.ScimGroupEdit(new ScimGroupMappingModel { Id = "sg1", CustomerGroupId = "g-admin" },
            _groups.Object, _stores.Object, _vendors.Object, _translation.Object);

        Assert.AreEqual("Scim", ((RedirectToActionResult)result).ActionName);
        _groups.Verify(x => x.SetMapping("sg1", "g-admin", null, null), Times.Once);
    }
}
