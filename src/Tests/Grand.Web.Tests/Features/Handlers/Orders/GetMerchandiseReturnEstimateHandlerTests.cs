using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Checkout.Orders;
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
public class GetMerchandiseReturnEstimateHandlerTests
{
    private readonly Order _order = new() { Id = "o1", StoreId = "s1", CustomerCurrencyCode = "EUR" };
    private GetMerchandiseReturnEstimateHandler _handler;
    private MerchandiseReturnSubmitResult _preview;
    private MerchandiseReturnSubmitRequest _request;
    private Dictionary<MerchandiseReturn, MerchandiseReturnRefundQuote> _quotes;

    [TestInitialize]
    public void Init()
    {
        _preview = new MerchandiseReturnSubmitResult();
        _quotes = [];
        var workflow = new Mock<IMerchandiseReturnWorkflowService>();
        workflow.Setup(x => x.Preview(It.IsAny<MerchandiseReturnSubmitRequest>()))
            .Callback<MerchandiseReturnSubmitRequest>(r => _request = r)
            .ReturnsAsync(() => _preview);
        var refunds = new Mock<IMerchandiseReturnRefundService>();
        refunds.Setup(x => x.GetQuote(It.IsAny<MerchandiseReturn>(), _order))
            .ReturnsAsync((MerchandiseReturn r, Order _) => _quotes[r]);
        var currencyService = new Mock<ICurrencyService>();
        currencyService.Setup(x => x.GetCurrencyByCode("EUR")).ReturnsAsync(new Currency { CurrencyCode = "EUR" });
        var priceFormatter = new Mock<IPriceFormatter>();
        priceFormatter.Setup(x => x.FormatPrice(It.IsAny<double>(), It.IsAny<Currency>()))
            .Returns((double price, Currency currency) => $"{currency?.CurrencyCode}{price}");
        var translationService = new Mock<ITranslationService>();
        translationService.Setup(x => x.GetResource(It.IsAny<string>(), "l1", It.IsAny<string>(), It.IsAny<bool>()))
            .Returns((string key, string _, string defaultValue, bool _) =>
                key == MerchandiseReturnErrors.SubmitQuantityExceeded ? "Taken meanwhile" : defaultValue);
        _handler = new GetMerchandiseReturnEstimateHandler(workflow.Object, refunds.Object, currencyService.Object,
            priceFormatter.Object, translationService.Object, new OrderSettings());
    }

    private void Previewed(double itemsAmount, double returnShippingCost, double amount)
    {
        var merchandiseReturn = new MerchandiseReturn();
        _preview.MerchandiseReturns.Add(merchandiseReturn);
        _quotes[merchandiseReturn] = new MerchandiseReturnRefundQuote {
            Error = MerchandiseReturnErrors.RefundNotAllowed, ItemsAmount = itemsAmount,
            ReturnShippingCost = returnShippingCost, Amount = amount
        };
    }

    private Task<MerchandiseReturnEstimateModel> Handle(Dictionary<string, string> form = null)
    {
        var model = new MerchandiseReturnModel
            { OrderId = "o1", ReturnShippingProvider = "Carrier", ReturnShippingOption = "parcel" };
        model.Items.Add(new MerchandiseReturnModel.OrderItemModel
            { Id = "a", Quantity = 2, MerchandiseReturnReasonId = "rr1" });
        return _handler.Handle(new GetMerchandiseReturnEstimate {
            Order = _order, Model = model, Customer = new Customer { Id = "c1" },
            Language = new Language { Id = "l1" }, Form = form ?? []
        }, CancellationToken.None);
    }

    [TestMethod]
    public async Task Estimate_SumsQuoteAmountsOfPreviewReturns()
    {
        //one return for the store's items and one for a vendor's
        Previewed(20, 3, 17);
        Previewed(5, 0, 5);

        var estimate = await Handle();

        Assert.AreEqual("EUR22", estimate.Amount);
        Assert.AreEqual("EUR25", estimate.ItemsAmount);
        Assert.AreEqual("EUR3", estimate.ReturnShippingCost);
        Assert.AreEqual(0, estimate.Errors.Count);
    }

    [TestMethod]
    public async Task Estimate_PreviewError_ErrorsAndNoAmount()
    {
        _preview.Error = MerchandiseReturnErrors.SubmitQuantityExceeded;
        _preview.ShippingErrors.Add("Pick a point.");

        var estimate = await Handle();

        CollectionAssert.AreEqual(new[] { "Taken meanwhile", "Pick a point." }, estimate.Errors.ToArray());
        Assert.IsNull(estimate.Amount);
    }

    [TestMethod]
    public async Task Estimate_PreviewsTheRequestTheSubmitSends()
    {
        await Handle(new Dictionary<string, string> { ["returnshipping.point"] = "p1", ["other"] = "x" });

        Assert.AreEqual("s1", _request.StoreId);
        Assert.AreEqual("c1", _request.CustomerId);
        Assert.AreEqual(new MerchandiseReturnSubmitItem("a", 2, "rr1", null), _request.Items.Single());
        Assert.AreEqual("Carrier", _request.ReturnShipping.ProviderSystemName);
        Assert.AreEqual("p1", _request.ReturnShipping.Form["point"]);
        Assert.IsFalse(_request.ReturnShipping.Form.ContainsKey("other"));
    }
}
