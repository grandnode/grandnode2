using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
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
        _handler = new GetMerchandiseReturnTokensCommandHandler(new Mock<ICountryService>().Object,
            new Mock<IProductService>().Object, currencyService.Object, priceFormatter.Object);
    }


    private async Task<Grand.Business.Messages.DotLiquidDrops.LiquidMerchandiseReturn> Build(
        MerchandiseReturnShipping shipping)
    {
        var merchandiseReturn = new MerchandiseReturn { Id = "r1", PickupAddress = new Address(), ReturnShipping = shipping };
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
