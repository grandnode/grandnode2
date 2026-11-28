using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Messages.Commands.Handlers.Messages;
using Grand.Business.Messages.Commands.Tokens;
using Grand.Domain.Common;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Business.Messages.Tests.Commands;

[TestClass]
public class GetMerchandiseReturnTokensCommandHandlerTests
{
    private readonly Currency _currency = new() { CurrencyCode = "EUR" };
    private GetMerchandiseReturnTokensCommandHandler _handler;

    public GetMerchandiseReturnTokensCommandHandlerTests()
    {
        var currencyService = new Mock<ICurrencyService>();
        currencyService.Setup(x => x.GetCurrencyByCode("EUR")).ReturnsAsync(_currency);
        var priceFormatter = new Mock<IPriceFormatter>();
        priceFormatter.Setup(x => x.FormatPrice(It.IsAny<double>(), _currency))
            .Returns<double, Currency>((amount, _) =>
                "EUR " + amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
        _handler = new GetMerchandiseReturnTokensCommandHandler(new Mock<ICountryService>().Object,
            new Mock<IProductService>().Object, currencyService.Object, priceFormatter.Object,
            new Mock<ITranslationService>().Object);
    }

    private Task<Grand.Business.Messages.DotLiquidDrops.LiquidMerchandiseReturn> Tokens(
        MerchandiseReturnStatus status, string? statusNote = null, MerchandiseReturnRefund? refund = null)
    {
        var merchandiseReturn = new MerchandiseReturn {
            MerchandiseReturnStatus = status, PickupAddress = new Address(), Refund = refund
        };
        return _handler.Handle(new GetMerchandiseReturnTokensCommand {
            MerchandiseReturn = merchandiseReturn,
            Order = new Order { CustomerCurrencyCode = "EUR" },
            Store = new Store { Url = "https://store.example/" },
            Host = new DomainHost { Url = "https://store.example/" },
            Language = new Language { Id = "en" },
            StatusNote = statusNote
        }, default);
    }

    [TestMethod]
    public async Task Rejected_RejectionReasonEncoded()
    {
        var tokens = await Tokens(MerchandiseReturnStatus.Rejected, "<b>Worn</b>");

        Assert.IsFalse(tokens.RejectionReason.Contains("<b>"));
        Assert.IsTrue(tokens.RejectionReason.Contains("Worn"));
    }

    [TestMethod]
    public async Task NotRejected_NoRejectionReason()
    {
        var tokens = await Tokens(MerchandiseReturnStatus.Accepted, "Worn");

        Assert.AreEqual("", tokens.RejectionReason);
    }

    [TestMethod]
    public async Task Refunded_RefundAmountInOrderCurrency()
    {
        var tokens = await Tokens(MerchandiseReturnStatus.Completed,
            refund: new MerchandiseReturnRefund { Amount = 49.5, RefundedOnUtc = DateTime.UtcNow });

        Assert.AreEqual("EUR 49.50", tokens.RefundAmount);
    }

    [TestMethod]
    public async Task RefundUnderWay_NoRefundAmount()
    {
        var tokens = await Tokens(MerchandiseReturnStatus.Received,
            refund: new MerchandiseReturnRefund { Amount = 49.5 });

        Assert.AreEqual("", tokens.RefundAmount);
    }
}
