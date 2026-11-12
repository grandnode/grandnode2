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

namespace Grand.Business.Messages.Tests.DotLiquidDrops;

[TestClass]
public class LiquidMerchandiseReturnTests
{
    private GetMerchandiseReturnTokensCommandHandler _handler;

    [TestInitialize]
    public void Init()
    {
        var currencyService = new Mock<ICurrencyService>();
        currencyService.Setup(x => x.GetCurrencyByCode("EUR")).ReturnsAsync(new Currency { CurrencyCode = "EUR" });
        var priceFormatter = new Mock<IPriceFormatter>();
        priceFormatter.Setup(x => x.FormatPrice(It.IsAny<double>(), It.IsAny<Currency>()))
            .Returns((double price, Currency currency) => $"{currency?.CurrencyCode}{price}");
        var translationService = new Mock<ITranslationService>();
        translationService.Setup(x => x.GetResource("MerchandiseReturns.ReturnShipping.Free", "1",
            It.IsAny<string>(), It.IsAny<bool>())).Returns("Gratis");
        _handler = new GetMerchandiseReturnTokensCommandHandler(new Mock<ICountryService>().Object,
            new Mock<IProductService>().Object, currencyService.Object, priceFormatter.Object,
            translationService.Object);
    }


    private async Task<Grand.Business.Messages.DotLiquidDrops.LiquidMerchandiseReturn> Build(
        MerchandiseReturnShipping shipping, MerchandiseReturnStatus status = MerchandiseReturnStatus.Pending)
    {
        var merchandiseReturn = new MerchandiseReturn {
            Id = "r1", PickupAddress = new Address(), ReturnShipping = shipping, MerchandiseReturnStatus = status
        };
        return await _handler.Handle(new GetMerchandiseReturnTokensCommand {
            MerchandiseReturn = merchandiseReturn,
            Order = new Order { CustomerCurrencyCode = "EUR" },
            Store = new Store { Url = "https://shop.test/" },
            Host = null,
            Language = new Language { Id = "1" }
        }, CancellationToken.None);
    }

    [TestMethod]
    public async Task ReturnShippingNull_TokensEmpty()
    {
        var drop = await Build(null);

        Assert.AreEqual("", drop.ReturnMethod);
        Assert.AreEqual("", drop.ReturnCost);
        Assert.AreEqual("", drop.ReturnTrackingNumber);
        Assert.AreEqual("", drop.ReturnCode);
        Assert.AreEqual("", drop.ReturnLabelUrl);
        Assert.AreEqual("", drop.ReturnInstructions);
    }

    [TestMethod]
    public async Task ReturnShipping_TokensFilled()
    {
        var drop = await Build(new MerchandiseReturnShipping {
            OptionName = "Parcel locker", Cost = 10, CurrencyRate = 2, TrackingNumber = "TRK1", ReturnCode = "C42",
            Instructions = "Leave it at the locker", LabelStatus = ReturnLabelStatus.Created, LabelDownloadId = "d1"
        });

        Assert.AreEqual("Parcel locker", drop.ReturnMethod);
        Assert.AreEqual("EUR20", drop.ReturnCost);
        Assert.AreEqual("TRK1", drop.ReturnTrackingNumber);
        Assert.AreEqual("C42", drop.ReturnCode);
        Assert.AreEqual("Leave it at the locker", drop.ReturnInstructions);
        Assert.AreEqual("https://shop.test/merchandisereturndetails/r1", drop.ReturnLabelUrl);
    }

    private static MerchandiseReturnShipping WithLabel(ReturnLabelStatus labelStatus = ReturnLabelStatus.Created)
    {
        return new MerchandiseReturnShipping {
            OptionName = "Parcel locker", TrackingNumber = "TRK1", ReturnCode = "C42",
            Instructions = "Leave it at the locker", LabelStatus = labelStatus, LabelDownloadId = "d1"
        };
    }

    [TestMethod]
    [DataRow(MerchandiseReturnStatus.Pending)]
    [DataRow(MerchandiseReturnStatus.Accepted)]
    public async Task ReturnShipping_StillToBeSent_HowToSendItBackFilled(MerchandiseReturnStatus status)
    {
        var drop = await Build(WithLabel(), status);

        Assert.AreEqual("C42", drop.ReturnCode);
        Assert.AreEqual("Leave it at the locker", drop.ReturnInstructions);
        Assert.AreEqual("https://shop.test/merchandisereturndetails/r1", drop.ReturnLabelUrl);
    }

    [TestMethod]
    [DataRow(MerchandiseReturnStatus.Rejected)]
    [DataRow(MerchandiseReturnStatus.Cancelled)]
    [DataRow(MerchandiseReturnStatus.Received)]
    [DataRow(MerchandiseReturnStatus.Completed)]
    public async Task ReturnShipping_NotToBeSentAnyMore_HowToSendItBackEmpty(MerchandiseReturnStatus status)
    {
        //the label of a rejected return whose cancellation failed is still Created: no link to it either
        var drop = await Build(WithLabel(), status);

        Assert.AreEqual("", drop.ReturnCode);
        Assert.AreEqual("", drop.ReturnInstructions);
        Assert.AreEqual("", drop.ReturnLabelUrl);
        //what the return was sent with stays
        Assert.AreEqual("Parcel locker", drop.ReturnMethod);
        Assert.AreEqual("TRK1", drop.ReturnTrackingNumber);
    }

    [TestMethod]
    public async Task ReturnShipping_LabelCancelled_HowToSendItBackEmpty()
    {
        var drop = await Build(WithLabel(ReturnLabelStatus.Cancelled), MerchandiseReturnStatus.Accepted);

        Assert.AreEqual("", drop.ReturnCode);
        Assert.AreEqual("", drop.ReturnInstructions);
        Assert.AreEqual("", drop.ReturnLabelUrl);
    }

    [TestMethod]
    public async Task ReturnShipping_NoCost_FreeInTheMessageLanguage()
    {
        var drop = await Build(new MerchandiseReturnShipping { OptionName = "Post", Cost = 0, CurrencyRate = 2 });

        Assert.AreEqual("Gratis", drop.ReturnCost);
    }

    [TestMethod]
    public async Task ReturnShipping_TextsHtmlEncoded()
    {
        var drop = await Build(new MerchandiseReturnShipping {
            OptionName = "<b>Post</b>", TrackingNumber = "<script>x</script>", ReturnCode = "A&B",
            Instructions = "Bring it to \"Shop\"\r\n<i>today</i>"
        });

        Assert.AreEqual("&lt;b&gt;Post&lt;/b&gt;", drop.ReturnMethod);
        Assert.AreEqual("&lt;script&gt;x&lt;/script&gt;", drop.ReturnTrackingNumber);
        Assert.AreEqual("A&amp;B", drop.ReturnCode);
        Assert.AreEqual("Bring it to &quot;Shop&quot;<br />&lt;i&gt;today&lt;/i&gt;", drop.ReturnInstructions);
    }

    [TestMethod]
    public async Task ReturnShipping_LabelNotCreated_LabelUrlEmpty()
    {
        var drop = await Build(new MerchandiseReturnShipping {
            OptionName = "Post", LabelStatus = ReturnLabelStatus.Failed, LabelError = "secret"
        });

        Assert.AreEqual("Post", drop.ReturnMethod);
        Assert.AreEqual("", drop.ReturnLabelUrl);
    }
}
