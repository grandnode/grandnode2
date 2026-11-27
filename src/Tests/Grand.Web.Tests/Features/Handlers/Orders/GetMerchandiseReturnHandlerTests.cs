using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.Web.Extensions;
using Grand.Web.Features.Handlers.Orders;
using Grand.Web.Features.Models.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Features.Handlers.Orders;

[TestClass]
public class GetMerchandiseReturnHandlerTests
{
    [TestMethod]
    public async Task GetMerchandiseReturnHandler_PhotoLimits_FromServer()
    {
        //Arrange - the view reads the limits from its model: a theme's copy of it cannot reference Grand.Web
        var storeContext = new Mock<IStoreContext>();
        storeContext.Setup(x => x.CurrentStore).Returns(new Domain.Stores.Store { Id = "s1" });
        var context = new Mock<IContextAccessor>();
        context.Setup(x => x.StoreContext).Returns(storeContext.Object);
        var returns = new Mock<IMerchandiseReturnService>();
        returns.Setup(x => x.GetAllMerchandiseReturnReasons(It.IsAny<string>())).ReturnsAsync([]);
        var mediator = new Mock<IMediator>();
        mediator.Setup(x => x.Send(It.IsAny<GetReturnableQuantitiesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int>());
        var returnShipping = new Mock<IReturnShippingService>();
        returnShipping.Setup(x => x.LoadActiveProviders(It.IsAny<Customer>(), It.IsAny<string>())).ReturnsAsync([]);
        var handler = new GetMerchandiseReturnHandler(context.Object, returns.Object,
            new Mock<IProductService>().Object, new Mock<IPriceFormatter>().Object, new Mock<ICountryService>().Object,
            new Mock<IAclService>().Object, new Mock<IVendorService>().Object, mediator.Object,
            new OrderSettings { MerchandiseReturns_HideRequestedAction = true }, new Mock<ICurrencyService>().Object,
            returnShipping.Object);

        //Act
        var model = await handler.Handle(new GetMerchandiseReturn { Language = new Grand.Domain.Localization.Language(), Store = new Grand.Domain.Stores.Store(),
            Order = new Order { Id = "o1", StoreId = "s1" }, Customer = new Customer { Id = "c1" }
        }, CancellationToken.None);

        //Assert
        Assert.AreEqual(MerchandiseReturnUploads.MaxFileBytes, model.MaxPhotoBytes);
        Assert.AreEqual(MerchandiseReturnUploads.MaxPhotosPerItem, model.MaxPhotosPerItem);
        Assert.AreEqual(MerchandiseReturnUploads.MaxPhotosPerSubmit, model.MaxPhotosPerSubmit);
    }
}
