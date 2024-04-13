using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Orders;

namespace DiscountRules.Standard.Providers;

public class HasOneProductDiscountRule : IDiscountRule
{
    private readonly ShoppingCartSettings _shoppingCartSettings;

    public HasOneProductDiscountRule(ShoppingCartSettings shoppingCartSettings)
    {
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

        //invalid by default
        var result = new DiscountRuleValidationResult();

        var restrictedProductIds = string.IsNullOrEmpty(request.DiscountRule.Metadata)
            ? []
            : request.DiscountRule.Metadata.Split(',').ToList();

        if (!restrictedProductIds.Any())
        {
            //valid
            result.IsValid = true;
            return result;
        }

        if (request.Customer == null)
            return result;

        //group products in the cart by product ID
        //it could be the same product with distinct product attributes
        //that's why we get the total quantity of this product
        var cartQuery =
            from sci in request.Customer.ShoppingCartItems.LimitPerStore(_shoppingCartSettings.SharedCartBetweenStores,
                request.Store.Id)
            where sci.ShoppingCartTypeId == ShoppingCartType.ShoppingCart
            group sci by sci.ProductId
            into g
            select new { ProductId = g.Key, TotalQuantity = g.Sum(x => x.Quantity) };
        var cart = cartQuery.ToList();

        //process
        var found = false;
        foreach (var restrictedProduct in restrictedProductIds)
        {
            if (string.IsNullOrWhiteSpace(restrictedProduct))
                continue;

            foreach (var sci in cart)
            {
                if (restrictedProduct.Contains(':'))
                {
                    if (restrictedProduct.Contains('-'))
                    {
                        //the third way (the quantity rage specified)
                        //{Product ID}:{Min quantity}-{Max quantity}. For example, 77:1-3, 123:2-5, 156:3-8
                        var restrictedProductId = restrictedProduct.Split([':'])[0];

                        if (!int.TryParse(restrictedProduct.Split([':'])[1].Split(['-'])[0], out var quantityMin))
                            //parsing error; exit;
                            return result;
                        if (!int.TryParse(restrictedProduct.Split([':'])[1].Split(['-'])[1], out var quantityMax))
                            //parsing error; exit;
                            return result;

                        if (sci.ProductId != restrictedProductId || quantityMin > sci.TotalQuantity ||
                            sci.TotalQuantity > quantityMax) continue;
                        found = true;
                        break;
                    }
                    else
                    {
                        //the second way (the quantity specified)
                        //{Product ID}:{Quantity}. For example, 77:1, 123:2, 156:3
                        var restrictedProductId = restrictedProduct.Split([':'])[0];

                        if (!int.TryParse(restrictedProduct.Split([':'])[1], out var quantity))
                            //parsing error; exit;
                            return result;

                        if (sci.ProductId != restrictedProductId || sci.TotalQuantity != quantity) continue;
                        found = true;
                        break;
                    }
                }

                //the first way (the quantity is not specified)
                if (sci.ProductId != restrictedProduct) continue;
                found = true;
                break;
            }

            if (found) break;
        }

        if (!found) return await Task.FromResult(result);
        //valid
        result.IsValid = true;
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
        var result = "Admin/HasOneProduct/Configure/?discountId=" + discountId;
        if (!string.IsNullOrEmpty(discountRequirementId))
            result += $"&discountRequirementId={discountRequirementId}";
        return result;
    }

    public string FriendlyName => "Customer has one of these products in the cart";

    public string SystemName => "DiscountRules.HasOneProduct";
}