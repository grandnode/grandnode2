using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Queries.Catalog;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Orders;
using Grand.Mediator;

namespace DiscountRules.Standard.Providers;

public class ShoppingCartDiscountRule : IDiscountRule
{
    private readonly IMediator _mediator;
    private readonly ShoppingCartSettings _shoppingCartSettings;

    public ShoppingCartDiscountRule(
        IMediator mediator,
        ShoppingCartSettings shoppingCartSettings)
    {
        _mediator = mediator;
        _shoppingCartSettings = shoppingCartSettings;
    }

    /// <summary>
    ///     Check discount requirement
    /// </summary>
    /// <param name="request">
    ///     Object that contains all information required to check the requirement (Current customer,
    ///     discount, etc)
    /// </param>
    /// <returns>true - requirement is met; otherwise, false</returns>
    public async Task<DiscountRuleValidationResult> CheckRequirement(DiscountRuleValidationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = new DiscountRuleValidationResult();

        if (!double.TryParse(request.DiscountRule.Metadata, out var spentAmountRequirement)) return result;

        if (spentAmountRequirement == 0)
        {
            result.IsValid = true;
            return result;
        }

        if (request.Customer == null) return result;

        var cart = request.Customer.ShoppingCartItems
            .Where(sci => sci.ShoppingCartTypeId == ShoppingCartType.ShoppingCart)
            .LimitPerStore(_shoppingCartSettings.SharedCartBetweenStores, request.Store.Id)
            .ToList();

        if (cart.Count == 0)
        {
            result.IsValid = false;
            return result;
        }

        var spentAmount = await _mediator.Send(new GetShoppingCartItemsSubTotalQuery {
            Customer = request.Customer,
            Store = request.Store,
            Items = cart
        });

        result.IsValid = spentAmount > spentAmountRequirement;
        return result;
    }

    /// <summary>
    ///     Get URL for rule configuration
    /// </summary>
    /// <param name="discountId">Discount identifier</param>
    /// <param name="discountRequirementId">Discount requirement identifier (if editing)</param>
    /// <returns>URL</returns>
    public string GetConfigurationUrl(string discountId, string discountRequirementId)
    {
        //configured 
        var result = $"/DiscountRulesShoppingCartAmount/Configure/?discountId=" + discountId;
        if (!string.IsNullOrEmpty(discountRequirementId))
            result += $"&discountRequirementId={discountRequirementId}";
        return result;
    }

    public string FriendlyName => "SubTotal in Shopping Cart x.xx ";
    public string SystemName => "DiscountRequirement.ShoppingCart";
}