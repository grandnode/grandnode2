using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Queries.Checkout.Orders;
using Grand.Domain;
using Grand.Domain.Common;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.Mediator;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.AdminShared.Services;
using Grand.Web.Common.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Admin.Tests.Services;

[TestClass]
public class OrderViewModelServiceMerchandiseReturnsTests
{
    private Mock<IMediator> _mediator;
    private Order _order;
    private Mock<IAdminDataScope<MerchandiseReturn>> _returnScope;
    private OrderViewModelService _service;

    [TestInitialize]
    public void Init()
    {
        _order = new Order { Id = "o1", StoreId = "s1" };
        _mediator = new Mock<IMediator>();
        _mediator.Setup(x => x.Send(It.IsAny<GetMerchandiseReturnQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedList<MerchandiseReturn>());
        _returnScope = new Mock<IAdminDataScope<MerchandiseReturn>>();
        var dateTime = new Mock<IDateTimeService>();
        dateTime.Setup(x => x.ConvertToUserTime(It.IsAny<DateTime>(), DateTimeKind.Utc))
            .Returns<DateTime, DateTimeKind>((date, _) => date);
        var enums = new Mock<IEnumTranslationService>();
        enums.Setup(x => x.GetTranslationEnum(It.IsAny<MerchandiseReturnStatus>()))
            .Returns<MerchandiseReturnStatus>(status => status.ToString());

        _service = new OrderViewModelService(new Mock<IOrderService>().Object, dateTime.Object,
            new Mock<IPriceFormatter>().Object, new Mock<IDiscountService>().Object,
            new Mock<IRequestTranslationService>().Object, new Mock<IContextAccessor>().Object,
            new Mock<ICurrencyService>().Object,
            new Mock<IPaymentService>().Object, new Mock<IPaymentTransactionService>().Object,
            new Mock<ICountryService>().Object, new Mock<IProductService>().Object,
            new Mock<IGiftVoucherService>().Object, new Mock<IDownloadService>().Object,
            new Mock<IStoreService>().Object, new Mock<IVendorService>().Object,
            new Mock<ISalesEmployeeService>().Object, new Mock<IAddressAttributeParser>().Object,
            new Mock<IAffiliateService>().Object, new Mock<IPictureService>().Object,
            new Mock<IMerchandiseReturnService>().Object, new Mock<ICustomerService>().Object,
            new Mock<IWarehouseService>().Object, new CurrencySettings(), new TaxSettings(), new AddressSettings(),
            new Mock<IOrderTagService>().Object, new Mock<IOrderStatusService>().Object, _mediator.Object,
            enums.Object, new Mock<IAdminDataScope<Order>>().Object, new Mock<IOrderApprovalService>().Object,
            new Mock<IShipmentService>().Object, _returnScope.Object);
    }

    private async Task<OrderModel> Prepare()
    {
        var model = new OrderModel();
        await _service.PrepareOrderMerchandiseReturns(model, _order);
        return model;
    }

    [TestMethod]
    public async Task PrepareOrderMerchandiseReturns_Admin_AllReturnsOfTheOrderInOneQuery()
    {
        await Prepare();

        _mediator.Verify(x => x.Send(It.Is<GetMerchandiseReturnQuery>(q =>
            q.OrderId == "o1" && q.StoreId == "" && q.VendorId == ""), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task PrepareOrderMerchandiseReturns_StorePanel_OnlyItsStoresReturns()
    {
        _returnScope.Setup(x => x.GetDefaultStoreIdAsync()).ReturnsAsync("s1");

        await Prepare();

        _mediator.Verify(x => x.Send(It.Is<GetMerchandiseReturnQuery>(q =>
            q.OrderId == "o1" && q.StoreId == "s1" && q.VendorId == ""), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task PrepareOrderMerchandiseReturns_Vendor_OnlyItsOwnReturns()
    {
        _returnScope.Setup(x => x.DefaultVendorId).Returns("v1");

        await Prepare();

        _mediator.Verify(x => x.Send(It.Is<GetMerchandiseReturnQuery>(q =>
            q.OrderId == "o1" && q.VendorId == "v1"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task PrepareOrderMerchandiseReturns_FillsTheRows()
    {
        var created = new DateTime(2026, 11, 10, 8, 0, 0, DateTimeKind.Utc);
        var withMethod = new MerchandiseReturn {
            Id = "r1", ReturnNumber = 7, CreatedOnUtc = created, MerchandiseReturnStatus = MerchandiseReturnStatus.Accepted,
            ReturnShipping = new MerchandiseReturnShipping { OptionName = "Parcel locker" },
            MerchandiseReturnItems = {
                new MerchandiseReturnItem { Quantity = 2 }, new MerchandiseReturnItem { Quantity = 1 }
            }
        };
        var withoutMethod = new MerchandiseReturn {
            Id = "r2", ReturnNumber = 8, CreatedOnUtc = created,
            MerchandiseReturnItems = { new MerchandiseReturnItem { Quantity = 1 } }
        };
        _mediator.Setup(x => x.Send(It.IsAny<GetMerchandiseReturnQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedList<MerchandiseReturn>(new[] { withMethod, withoutMethod }, 0, 10));

        var model = await Prepare();

        Assert.AreEqual(2, model.MerchandiseReturns.Count);
        var row = model.MerchandiseReturns[0];
        Assert.AreEqual("r1", row.Id);
        Assert.AreEqual(7, row.ReturnNumber);
        Assert.AreEqual(created, row.CreatedOn);
        Assert.AreEqual("Accepted", row.Status);
        Assert.AreEqual("Parcel locker", row.ReturnMethod);
        Assert.AreEqual(3, row.Quantity);
        Assert.AreEqual("", model.MerchandiseReturns[1].ReturnMethod);
    }
}
