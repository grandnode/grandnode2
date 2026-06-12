using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Models.Shipping;
using Grand.Web.Store.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Store.Tests.Controllers;

[TestClass]
public class ShippingControllerTests
{
    private const string StoreId = "storeId";

    private ShippingController _controller;
    private Mock<IContextAccessor> _contextAccessorMock;
    private Mock<ICountryService> _countryServiceMock;
    private Mock<IGroupService> _groupServiceMock;
    private Mock<IShippingMethodService> _shippingMethodServiceMock;
    private Mock<ITranslationService> _translationServiceMock;

    [TestInitialize]
    public void Setup()
    {
        _shippingMethodServiceMock = new Mock<IShippingMethodService>();
        _countryServiceMock = new Mock<ICountryService>();
        _groupServiceMock = new Mock<IGroupService>();
        _translationServiceMock = new Mock<ITranslationService>();
        _translationServiceMock.Setup(t => t.GetResource(It.IsAny<string>())).Returns("resource");

        var workContextMock = new Mock<IWorkContext>();
        workContextMock.Setup(w => w.CurrentCustomer).Returns(new Customer { StaffStoreId = StoreId });
        _contextAccessorMock = new Mock<IContextAccessor>();
        _contextAccessorMock.Setup(c => c.WorkContext).Returns(workContextMock.Object);

        _controller = new ShippingController(
            new Mock<IShippingService>().Object,
            _shippingMethodServiceMock.Object,
            new Mock<IDeliveryDateService>().Object,
            new Mock<IWarehouseService>().Object,
            new Mock<IPickupPointService>().Object,
            _countryServiceMock.Object,
            _groupServiceMock.Object,
            new Mock<ILanguageService>().Object,
            _translationServiceMock.Object,
            new Mock<ISettingService>().Object,
            _contextAccessorMock.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>());
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        _controller.TempData = new TempDataDictionary(httpContext, new Mock<ITempDataProvider>().Object);
    }

    private void SetupCommonData(Country country, ShippingMethod shippingMethod, CustomerGroup customerGroup)
    {
        _countryServiceMock.Setup(c => c.GetAllCountries(It.IsAny<string>(), It.IsAny<string>(), true))
            .ReturnsAsync(new List<Country> { country });
        _shippingMethodServiceMock.Setup(s => s.GetAllShippingMethods(It.IsAny<string>(), null, StoreId))
            .ReturnsAsync(new List<ShippingMethod> { shippingMethod });
        _groupServiceMock.Setup(g =>
                g.GetAllCustomerGroups(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
            .ReturnsAsync(new PagedList<CustomerGroup> { customerGroup });
    }

    [TestMethod]
    public async Task Restrictions_Get_BuildModelFromStoreScopedShippingMethods()
    {
        var country = new Country { Id = "countryId", Name = "Poland" };
        var customerGroup = new CustomerGroup { Name = "Guests" };
        var shippingMethod = new ShippingMethod { Name = "Ground" };
        shippingMethod.RestrictedCountries.Add(country);
        shippingMethod.RestrictedGroups.Add(customerGroup.Id);
        SetupCommonData(country, shippingMethod, customerGroup);

        var result = await _controller.Restrictions();

        var viewResult = result as ViewResult;
        Assert.IsNotNull(viewResult);
        var model = viewResult.Model as ShippingMethodRestrictionModel;
        Assert.IsNotNull(model);
        Assert.IsTrue(model.Restricted["countryId"][shippingMethod.Id]);
        Assert.IsTrue(model.RestictedGroup[customerGroup.Id][shippingMethod.Id]);
        _shippingMethodServiceMock.Verify(s => s.GetAllShippingMethods(It.IsAny<string>(), null, StoreId),
            Times.Once);
    }

    [TestMethod]
    public async Task RestrictionSave_AddRestriction_UpdateShippingMethod()
    {
        var country = new Country { Id = "countryId", Name = "Poland" };
        var customerGroup = new CustomerGroup { Name = "Guests" };
        var shippingMethod = new ShippingMethod { Name = "Ground" };
        SetupCommonData(country, shippingMethod, customerGroup);

        var form = new Dictionary<string, string[]> {
            [$"restrict_{shippingMethod.Id}"] = ["countryId"]
        };

        var result = await _controller.RestrictionSave(form);

        var redirect = result as RedirectToActionResult;
        Assert.IsNotNull(redirect);
        Assert.AreEqual("Restrictions", redirect.ActionName);
        Assert.IsTrue(shippingMethod.RestrictedCountries.Any(c => c.Id == "countryId"));
        _shippingMethodServiceMock.Verify(s => s.UpdateShippingMethod(shippingMethod), Times.Once);
        _shippingMethodServiceMock.Verify(s => s.GetAllShippingMethods(It.IsAny<string>(), null, StoreId),
            Times.Once);
    }

    [TestMethod]
    public async Task RestrictionSave_NoFormValues_ClearExistingRestrictions()
    {
        var country = new Country { Id = "countryId", Name = "Poland" };
        var customerGroup = new CustomerGroup { Name = "Guests" };
        var shippingMethod = new ShippingMethod { Name = "Ground" };
        shippingMethod.RestrictedCountries.Add(country);
        shippingMethod.RestrictedGroups.Add(customerGroup.Id);
        SetupCommonData(country, shippingMethod, customerGroup);

        var result = await _controller.RestrictionSave(new Dictionary<string, string[]>());

        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        Assert.IsEmpty(shippingMethod.RestrictedCountries);
        Assert.IsEmpty(shippingMethod.RestrictedGroups);
        _shippingMethodServiceMock.Verify(s => s.UpdateShippingMethod(shippingMethod), Times.Exactly(2));
    }

    [TestMethod]
    public async Task RestrictionSave_NoChanges_NotUpdateShippingMethod()
    {
        var country = new Country { Id = "countryId", Name = "Poland" };
        var customerGroup = new CustomerGroup { Name = "Guests" };
        var shippingMethod = new ShippingMethod { Name = "Ground" };
        SetupCommonData(country, shippingMethod, customerGroup);

        var result = await _controller.RestrictionSave(new Dictionary<string, string[]>());

        Assert.IsInstanceOfType<RedirectToActionResult>(result);
        _shippingMethodServiceMock.Verify(s => s.UpdateShippingMethod(It.IsAny<ShippingMethod>()), Times.Never);
    }
}
