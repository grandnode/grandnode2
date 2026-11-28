using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Infrastructure;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Shipping.ShippingPoint.Controllers;
using Shipping.ShippingPoint.Services;

namespace Grand.Modules.Tests.Plugins.Shipping;

[TestClass]
public class SelectedShippingPointControllerTests
{
    private readonly SelectedShippingPointController _controller;

    public SelectedShippingPointControllerTests()
    {
        var translation = new Mock<IRequestTranslationService>();
        translation.Setup(x => x.GetResource("Shipping.ShippingPoint.PluginName")).Returns("Shipping point");
        _controller = new SelectedShippingPointController(new Mock<IShippingPointService>().Object,
            new Mock<ICountryService>().Object, new Mock<IPriceFormatter>().Object, new Mock<IContextAccessor>().Object,
            new Mock<ICurrencyService>().Object, translation.Object, new Mock<IOrderService>().Object,
            new Mock<IGroupService>().Object);
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow(":")]
    [DataRow("Ground:Shipping.FixedRate")]
    public async Task Points_NoShippingPointOption_SaysTheOptionDoesNotExist(string shippingOption)
    {
        //a public endpoint: a request without the option, or with another one, answers without a server error
        var result = await _controller.Points(shippingOption);

        Assert.AreEqual("ShippingPointController: given Shipping Option doesn't exist",
            (result as ContentResult)?.Content);
    }
}
