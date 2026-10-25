using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.CheckoutAttributes;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Localization;
using Grand.Domain.Media;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Domain.Tax;
using Grand.Infrastructure.Configuration;
using Grand.Mediator;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Features.Handlers.ShoppingCart;
using Grand.Web.Features.Models.ShoppingCart;
using Grand.Web.Models.ShoppingCart;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Web.Tests.Features.ShoppingCart;

[TestClass]
public class GetShoppingCartHandlerApprovalTests
{
    private static readonly DateTime Created = new(2026, 10, 20, 8, 30, 0, DateTimeKind.Utc);
    private static readonly DateTime CreatedForUser = new(2026, 10, 20, 10, 30, 0);

    private Mock<IOrderApprovalService> _approvals;
    private CustomerSettings _customerSettings;
    private Customer _customer;
    private List<ShoppingCartItem> _cart;
    private GetShoppingCartHandler _handler;

    [TestInitialize]
    public void Init()
    {
        _customer = new Customer { Id = "buyer", OwnerId = "owner" };
        _cart = [new ShoppingCartItem {
            Id = "i1", ProductId = "p1", Quantity = 2, ShoppingCartTypeId = ShoppingCartType.ShoppingCart
        }];
        _approvals = new Mock<IOrderApprovalService>();
        _customerSettings = new CustomerSettings();

        var dates = new Mock<IDateTimeService>();
        dates.Setup(d => d.ConvertToUserTime(Created, DateTimeKind.Utc)).Returns(CreatedForUser);
        var translation = new Mock<IRequestTranslationService>();
        translation.Setup(t => t.GetResource(It.IsAny<string>())).Returns((string key) => key + "|{0}");
        var checkoutAttributes = new Mock<ICheckoutAttributeService>();
        checkoutAttributes.Setup(c => c.GetAllCheckoutAttributes(It.IsAny<CatalogAccess>(), It.IsAny<bool>()))
            .ReturnsAsync([]);
        var validator = new Mock<IShoppingCartValidator>();
        validator.Setup(v => v.GetShoppingCartWarnings(It.IsAny<IList<ShoppingCartItem>>(),
                It.IsAny<IList<Grand.Domain.Common.CustomAttribute>>(), It.IsAny<CalculationContext>(),
                It.IsAny<Language>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync([]);
        var groups = new Mock<IGroupService>();

        _handler = new GetShoppingCartHandler(new Mock<IProductService>().Object,
            new Mock<IPictureService>().Object, translation.Object, new Mock<ICheckoutAttributeFormatter>().Object,
            new Mock<ICurrencyService>().Object, new Mock<IDiscountService>().Object, checkoutAttributes.Object,
            new Mock<IRequestPermissionService>().Object, new Mock<ITaxService>().Object,
            new Mock<IPriceFormatter>().Object, new Mock<ICheckoutAttributeParser>().Object,
            new Mock<IDownloadService>().Object, new Mock<IWarehouseService>().Object,
            new Mock<IProductAttributeFormatter>().Object, new Mock<IPricingService>().Object, dates.Object,
            new Mock<IVendorService>().Object, groups.Object, new Mock<IMediator>().Object, validator.Object,
            new Mock<IDiscountValidationService>().Object, new Mock<LinkGenerator>().Object, new MediaSettings(),
            new OrderSettings(), new OidcLoginConfig(), new ShoppingCartSettings(), new CatalogSettings(),
            new CommonSettings(), new Mock<IEnumTranslationService>().Object, _approvals.Object, _customerSettings);
    }

    private Task<ShoppingCartModel> Cart()
    {
        return _handler.Handle(new GetShoppingCart {
            Customer = _customer,
            Store = new Grand.Domain.Stores.Store { Id = "s1" },
            Language = new Language { Id = "en" },
            Currency = new Currency { Id = "c1" },
            Host = new DomainHost(),
            Cart = _cart
        }, CancellationToken.None);
    }

    [TestMethod]
    public async Task Cart_PendingApproval_ShowsNote()
    {
        _approvals.Setup(a => a.GetActive("buyer", "s1")).ReturnsAsync(new OrderApproval {
            Status = OrderApprovalStatus.Pending, CreatedOnUtc = Created
        });

        var model = await Cart();

        StringAssert.Contains(model.OrderApprovalNote, CreatedForUser.ToString("g"));
        StringAssert.StartsWith(model.OrderApprovalNote, "OrderApproval.Cart.Pending");
    }

    [TestMethod]
    public async Task Cart_ApprovedForThisCart_ShowsApprovedNote()
    {
        _approvals.Setup(a => a.GetActive("buyer", "s1")).ReturnsAsync(new OrderApproval {
            Status = OrderApprovalStatus.Approved, CreatedOnUtc = Created,
            CartFingerprint = OrderApprovalFingerprint.Compute(_cart)
        });

        var model = await Cart();

        StringAssert.StartsWith(model.OrderApprovalNote, "OrderApproval.Cart.Approved");
    }

    [TestMethod]
    public async Task Cart_ApprovedForAnotherCart_NoNote()
    {
        _approvals.Setup(a => a.GetActive("buyer", "s1")).ReturnsAsync(new OrderApproval {
            Status = OrderApprovalStatus.Approved, CreatedOnUtc = Created, CartFingerprint = "other"
        });

        Assert.IsNull((await Cart()).OrderApprovalNote);
    }

    [TestMethod]
    public async Task Cart_NoApproval_NoNote()
    {
        _approvals.Setup(a => a.GetActive(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((OrderApproval)null);

        Assert.IsNull((await Cart()).OrderApprovalNote);
    }

    [TestMethod]
    public async Task Cart_NotACompanyBuyer_NoNoteAndNoLookup()
    {
        _customer.OwnerId = null;

        var model = await Cart();

        Assert.IsNull(model.OrderApprovalNote);
        _approvals.Verify(a => a.GetActive(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Cart_FeatureOff_NoNoteAndNoLookup()
    {
        _customerSettings.HideSubAccountsTab = true;

        var model = await Cart();

        Assert.IsNull(model.OrderApprovalNote);
        _approvals.Verify(a => a.GetActive(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
