using DiscountRules.Standard.Providers;
using Grand.Business.Core.Queries.Catalog;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Discounts;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Mediator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Grand.Modules.Tests.Plugins.DiscountRules;

[TestClass]
public class ShoppingCartDiscountRuleTests
{
    private Mock<IMediator> _mediator;
    private ShoppingCartDiscountRule _rule;

    [TestInitialize]
    public void Init()
    {
        _mediator = new Mock<IMediator>();
        _rule = new ShoppingCartDiscountRule(_mediator.Object, new ShoppingCartSettings());
    }

    private static DiscountRuleValidationRequest Request(Customer customer, string spentAmount = "100")
    {
        return new DiscountRuleValidationRequest {
            Discount = new Discount(),
            DiscountRule = new DiscountRule { Metadata = spentAmount },
            Customer = customer,
            Store = new Store { Id = "store1" }
        };
    }

    [TestMethod]
    public async Task CheckRequirement_SumsCartOfRequestCustomerInRequestStore()
    {
        var customer = new Customer();
        var inStore = new ShoppingCartItem { ShoppingCartTypeId = ShoppingCartType.ShoppingCart, StoreId = "store1" };
        customer.ShoppingCartItems.Add(inStore);
        customer.ShoppingCartItems.Add(new ShoppingCartItem { ShoppingCartTypeId = ShoppingCartType.ShoppingCart, StoreId = "store2" });
        customer.ShoppingCartItems.Add(new ShoppingCartItem { ShoppingCartTypeId = ShoppingCartType.Wishlist, StoreId = "store1" });
        _mediator.Setup(m => m.Send(It.IsAny<GetShoppingCartItemsSubTotalQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(150d);

        var result = await _rule.CheckRequirement(Request(customer));

        Assert.IsTrue(result.IsValid);
        _mediator.Verify(m => m.Send(
            It.Is<GetShoppingCartItemsSubTotalQuery>(q => q.Items.Count == 1 && q.Items[0] == inStore),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [TestMethod]
    public async Task CheckRequirement_SubtotalNotAboveRequirement_IsNotValid()
    {
        var customer = new Customer();
        customer.ShoppingCartItems.Add(new ShoppingCartItem { ShoppingCartTypeId = ShoppingCartType.ShoppingCart, StoreId = "store1" });
        _mediator.Setup(m => m.Send(It.IsAny<GetShoppingCartItemsSubTotalQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(100d);

        var result = await _rule.CheckRequirement(Request(customer));

        Assert.IsFalse(result.IsValid);
    }

    [TestMethod]
    public async Task CheckRequirement_NoCustomer_IsNotValid()
    {
        var result = await _rule.CheckRequirement(Request(null));

        Assert.IsFalse(result.IsValid);
        _mediator.Verify(m => m.Send(It.IsAny<GetShoppingCartItemsSubTotalQuery>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
