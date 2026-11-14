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
    private MerchandiseReturnRefundQuote _estimate;

    [TestInitialize]
    public void Init()
    {
        _preview = new MerchandiseReturnSubmitResult();
        _estimate = new MerchandiseReturnRefundQuote();
        var workflow = new Mock<IMerchandiseReturnWorkflowService>();
        workflow.Setup(x => x.Preview(It.IsAny<MerchandiseReturnSubmitRequest>()))
            .Callback<MerchandiseReturnSubmitRequest>(r => _request = r)
            .ReturnsAsync(() => _preview);
        var refunds = new Mock<IMerchandiseReturnRefundService>();
        refunds.Setup(x => x.GetEstimate(It.IsAny<IList<MerchandiseReturn>>(), _order))
            .ReturnsAsync((IList<MerchandiseReturn> returns, Order _) =>
                returns.SequenceEqual(_preview.MerchandiseReturns) ? _estimate : null);
        var currencyService = new Mock<ICurrencyService>();
        currencyService.Setup(x => x.GetCurrencyByCode("EUR")).ReturnsAsync(new Currency { CurrencyCode = "EUR" });
        var priceFormatter = new Mock<IPriceFormatter>();
        priceFormatter.Setup(x => x.FormatPrice(It.IsAny<double>(), It.IsAny<Currency>()))
            .Returns((double price, Currency currency) => $"{currency?.CurrencyCode}{price}");
        var translationService = new Mock<ITranslationService>();
        translationService.Setup(x => x.GetResource(It.IsAny<string>(), "l1", It.IsAny<string>(), It.IsAny<bool>()))
            .Returns((string key, string _, string defaultValue, bool _) =>
                key == MerchandiseReturnErrors.SubmitQuantityExceeded ? "Taken meanwhile" : defaultValue);
        var groupService = new Mock<IGroupService>();
        groupService.Setup(x => x.IsOwner(It.Is<Customer>(c => c.Id == "c1"))).ReturnsAsync(true);
        _handler = new GetMerchandiseReturnEstimateHandler(workflow.Object, refunds.Object, groupService.Object,
            currencyService.Object, priceFormatter.Object, translationService.Object, new OrderSettings());
    }

    private Task<MerchandiseReturnEstimateModel> Handle(Dictionary<string, string> form = null)
    {
        var model = new MerchandiseReturnModel {
            OrderId = "o1", ReturnShippingProvider = "Carrier", ReturnShippingOption = "parcel", Comments = "torn"
        };
        model.Items.Add(new MerchandiseReturnModel.OrderItemModel
            { Id = "a", Quantity = 2, MerchandiseReturnReasonId = "rr1" });
        return _handler.Handle(new GetMerchandiseReturnEstimate {
            Order = _order, Model = model, Customer = new Customer { Id = "c1" },
            Language = new Language { Id = "l1" }, Form = form ?? []
        }, CancellationToken.None);
    }

    [TestMethod]
    public async Task Estimate_OfThePreviewReturns_AtMostWhatGoesBack()
    {
        //one return for the store's items and one for a vendor's; the payment has 15 left to refund
        _preview.MerchandiseReturns.Add(new MerchandiseReturn());
        _preview.MerchandiseReturns.Add(new MerchandiseReturn { VendorId = "v1" });
        _estimate.ItemsAmount = 25;
        _estimate.ReturnShippingCost = 3;
        _estimate.Amount = 22;
        _estimate.MaxAmount = 15;
        _estimate.CanRefundOnline = true;

        var estimate = await Handle();

        Assert.AreEqual("EUR15", estimate.Amount);
        Assert.AreEqual("EUR25", estimate.ItemsAmount);
        Assert.AreEqual("EUR3", estimate.ReturnShippingCost);
        Assert.IsTrue(estimate.ToPaymentMethod);
        Assert.AreEqual(0, estimate.Errors.Count);
    }

    [TestMethod]
    public async Task Estimate_PaymentProviderCannotRefund_NotToThePaymentMethod()
    {
        _preview.MerchandiseReturns.Add(new MerchandiseReturn());
        _estimate.MaxAmount = 10;

        var estimate = await Handle();

        Assert.IsFalse(estimate.ToPaymentMethod);
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
        Assert.AreEqual("c1", _request.OwnerId);
        Assert.AreEqual("l1", _request.LanguageId);
        Assert.AreEqual("torn", _request.Comments);
        Assert.AreEqual(new MerchandiseReturnSubmitItem("a", 2, "rr1", null), _request.Items.Single());
        Assert.AreEqual("Carrier", _request.ReturnShipping.ProviderSystemName);
        Assert.AreEqual("p1", _request.ReturnShipping.Form["point"]);
        Assert.IsFalse(_request.ReturnShipping.Form.ContainsKey("other"));
    }
}
