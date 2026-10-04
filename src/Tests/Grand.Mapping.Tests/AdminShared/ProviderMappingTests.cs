using Grand.Mapping;
using Grand.Business.Core.Interfaces.Authentication;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Cms;
using Grand.Infrastructure.Plugins;
using Grand.Web.AdminShared.Mapper;
using Grand.Web.AdminShared.Models.Cms;
using Grand.Web.AdminShared.Models.ExternalAuthentication;
using Grand.Web.AdminShared.Models.Payments;
using Grand.Web.AdminShared.Models.Plugins;
using Grand.Web.AdminShared.Models.Shipping;
using Grand.Web.AdminShared.Models.Tax;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using VerifyMSTest;

namespace Grand.Mapping.Tests.AdminShared;

[TestClass]
public class ProviderMappingTests : VerifyBase
{
    private IMapper _mapper;

    [TestInitialize]
    public void Setup()
    {
        var config = new MapperConfiguration(cfg => {
            cfg.AddProfile<PaymentMethodProfile>();
            cfg.AddProfile<TaxProviderProfile>();
            cfg.AddProfile<ShippingRateComputationMethodProfile>();
            cfg.AddProfile<WidgetPluginProfile>();
            cfg.AddProfile<ExternalAuthenticationMethodProfile>();
            cfg.AddProfile<PluginDescriptorProfile>();
        });
        _mapper = config.CreateMapper();
    }

    // ── PaymentMethod ─────────────────────────────────────────────────────────

    [TestMethod]
    public Task PaymentProvider_ToModel()
    {
        var mock = new Mock<IPaymentProvider>();
        mock.Setup(x => x.FriendlyName).Returns("Cash on Delivery");
        mock.Setup(x => x.SystemName).Returns("Payments.CashOnDelivery");
        mock.Setup(x => x.Priority).Returns(1);

        return Verify(_mapper.Map<IPaymentProvider, PaymentMethodModel>(mock.Object));
    }

    // ── TaxProvider ───────────────────────────────────────────────────────────

    [TestMethod]
    public Task TaxProvider_ToModel()
    {
        var mock = new Mock<ITaxProvider>();
        mock.Setup(x => x.FriendlyName).Returns("Fixed Rate");
        mock.Setup(x => x.SystemName).Returns("Tax.FixedRate");

        return Verify(_mapper.Map<ITaxProvider, TaxProviderModel>(mock.Object));
    }

    // ── ShippingRateComputationMethod ─────────────────────────────────────────

    [TestMethod]
    public Task ShippingRateCalculationProvider_ToModel()
    {
        var mock = new Mock<IShippingRateCalculationProvider>();
        mock.Setup(x => x.FriendlyName).Returns("Fixed Rate Shipping");
        mock.Setup(x => x.SystemName).Returns("Shipping.FixedRate");
        mock.Setup(x => x.Priority).Returns(5);
        mock.Setup(x => x.ConfigurationUrl).Returns("../ShippingFixedRate/Configure");

        return Verify(_mapper.Map<IShippingRateCalculationProvider, ShippingRateComputationMethodModel>(mock.Object));
    }

    // ── WidgetPlugin ──────────────────────────────────────────────────────────

    [TestMethod]
    public Task WidgetProvider_ToModel()
    {
        var mock = new Mock<IWidgetProvider>();
        mock.Setup(x => x.FriendlyName).Returns("Bootstrap Slider");
        mock.Setup(x => x.SystemName).Returns("Widgets.Slider");
        mock.Setup(x => x.Priority).Returns(0);

        return Verify(_mapper.Map<IWidgetProvider, WidgetModel>(mock.Object));
    }

    // ── ExternalAuthentication ────────────────────────────────────────────────

    [TestMethod]
    public Task ExternalAuthenticationProvider_ToModel()
    {
        var mock = new Mock<IExternalAuthenticationProvider>();
        mock.Setup(x => x.FriendlyName).Returns("Facebook authentication");
        mock.Setup(x => x.SystemName).Returns("ExternalAuth.Facebook");
        mock.Setup(x => x.Priority).Returns(0);

        return Verify(_mapper.Map<IExternalAuthenticationProvider, AuthenticationMethodModel>(mock.Object));
    }

    // ── PluginDescriptor ──────────────────────────────────────────────────────

    [TestMethod]
    public Task PluginInfo_ToModel()
    {
        var pluginInfo = new PluginInfo {
            FriendlyName = "Bootstrap Slider",
            Group = "Widgets",
            SystemName = "Widgets.Slider",
            Version = "2.1.2",
            Author = "grandnode team",
            DisplayOrder = 0,
            Installed = true
        };

        return Verify(_mapper.Map<PluginModel>(pluginInfo));
    }
}
