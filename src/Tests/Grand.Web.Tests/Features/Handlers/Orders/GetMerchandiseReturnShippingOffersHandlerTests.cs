using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Web.Features.Handlers.Orders;
using Grand.Web.Features.Models.Orders;
using Grand.Web.Models.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Features.Handlers.Orders;

[TestClass]
public class GetMerchandiseReturnShippingOffersHandlerTests
{
    private readonly Order _order = new() { Id = "o1", StoreId = "s1", CustomerCurrencyCode = "EUR", CurrencyRate = 2 };
    private string _fromText;
    private GetMerchandiseReturnShippingOffersHandler _handler;
    private List<ReturnShippingOffer> _offers;

    [TestInitialize]
    public void Init()
    {
        _offers = [];
        _fromText = "from {0}";
        var returnShippingService = new Mock<IReturnShippingService>();
        returnShippingService.Setup(x => x.GetOffers(It.IsAny<MerchandiseReturnSubmitRequest>()))
            .ReturnsAsync(() => _offers);
        var currencyService = new Mock<ICurrencyService>();
        currencyService.Setup(x => x.GetCurrencyByCode("EUR")).ReturnsAsync(new Currency { CurrencyCode = "EUR" });
        var priceFormatter = new Mock<IPriceFormatter>();
        priceFormatter.Setup(x => x.FormatPrice(It.IsAny<double>(), It.IsAny<Currency>()))
            .Returns((double price, Currency currency) => $"{currency?.CurrencyCode}{price}");
        var translationService = new Mock<ITranslationService>();
        translationService.Setup(x => x.GetResource(It.IsAny<string>(), "l1", It.IsAny<string>(), It.IsAny<bool>()))
            .Returns((string key, string _, string _, bool _) => key switch {
                "MerchandiseReturns.ReturnShipping.Free" => "Free",
                "MerchandiseReturns.ReturnShipping.From" => _fromText,
                _ => key
            });
        _handler = new GetMerchandiseReturnShippingOffersHandler(returnShippingService.Object,
            currencyService.Object, priceFormatter.Object, translationService.Object);
    }

    private async Task<(IList<ReturnShippingOfferModel> Models, IDictionary<string, object> FormValues)> Handle()
    {
        IDictionary<string, object> formValues = null;
        var models = await _handler.Handle(new GetMerchandiseReturnShippingOffers {
            Order = _order,
            Model = new MerchandiseReturnModel(),
            Customer = new Customer { Id = "c1" },
            Language = new Language { Id = "l1" },
            FormUrl = (routeName, values) =>
            {
                formValues = values;
                return "/" + routeName;
            }
        }, CancellationToken.None);
        return (models, formValues);
    }

    private static ReturnShippingOffer Offer(double cost, bool costVaries = false)
    {
        return new ReturnShippingOffer {
            ProviderSystemName = "Return.Point", OptionCode = "point", Name = "Point", Cost = cost,
            CostVaries = costVaries, Parcels = 1, HasForm = true, FormRouteName = "Point.Form"
        };
    }

    [TestMethod]
    public async Task Offer_WithForm_FormUrlCarriesTheOrder()
    {
        _offers.Add(Offer(3));

        var (models, formValues) = await Handle();

        Assert.AreEqual("/Point.Form", models[0].FormUrl);
        Assert.AreEqual("o1", formValues["orderId"]);
        Assert.IsFalse(formValues.ContainsKey("free"));
        Assert.AreEqual("EUR6", models[0].Cost);
    }

    [TestMethod]
    public async Task Offer_FreeWhateverIsChosen_FormToldFree()
    {
        _offers.Add(Offer(0));

        var (models, formValues) = await Handle();

        Assert.AreEqual("Free", models[0].Cost);
        Assert.AreEqual(true, formValues["free"]);
    }

    [TestMethod]
    public async Task Offer_CostVaries_ShownFromTheLowestAndFormNotFree()
    {
        _offers.Add(Offer(0, true));

        var (models, formValues) = await Handle();

        Assert.AreEqual("from EUR0", models[0].Cost);
        Assert.IsFalse(formValues.ContainsKey("free"));
    }

    [TestMethod]
    public async Task Offer_TranslationEditedWrong_ShowsTheCostAlone()
    {
        //a stray brace typed into the translation: the form still lists the method
        _fromText = "from {0";
        _offers.Add(Offer(3, true));

        var (models, _) = await Handle();

        Assert.AreEqual("EUR6", models[0].Cost);
    }
}
