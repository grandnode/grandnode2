using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace DiscountRules.Standard.Providers;

public class ShoppingCartDiscountRule : IDiscountRule
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IProductService _productService;
    private readonly ShoppingCartSettings _shoppingCartSettings;
    private readonly IWorkContext _workContext;

    public ShoppingCartDiscountRule(
        IWorkContext workContext,
        IProductService productService,
        IHttpContextAccessor httpContextAccessor,
        ShoppingCartSettings shoppingCartSettings)
    {
        _workContext = workContext;
        _productService = productService;
        _httpContextAccessor = httpContextAccessor;
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

        var cart = _workContext.CurrentCustomer.ShoppingCartItems
            .Where(sci => sci.ShoppingCartTypeId == ShoppingCartType.ShoppingCart)
            .LimitPerStore(_shoppingCartSettings.SharedCartBetweenStores, request.Store.Id)
            .ToList();

        if (cart.Count == 0)
        {
            result.IsValid = false;
            return result;
        }

        double spentAmount = 0;

        var priceCalculationService =
            _httpContextAccessor.HttpContext!.RequestServices.GetRequiredService<IPricingService>();

        foreach (var ca in cart)
        {
            var product = await _productService.GetProductById(ca.ProductId);
            if (product != null)
                spentAmount += (await priceCalculationService.GetSubTotal(ca, product, false)).subTotal;
        }

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
        var result = "Admin/ShoppingCartAmount/Configure/?discountId=" + discountId;
        if (!string.IsNullOrEmpty(discountRequirementId))
            result += $"&discountRequirementId={discountRequirementId}";
        return result;
    }

    public string FriendlyName => "SubTotal in Shopping Cart x.xx ";
    public string SystemName => "DiscountRequirement.ShoppingCart";
}