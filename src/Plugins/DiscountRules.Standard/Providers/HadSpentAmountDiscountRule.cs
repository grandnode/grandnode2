using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Orders;

namespace DiscountRules.Standard.Providers;

public class HadSpentAmountDiscountRule : IDiscountRule
{
    private readonly IOrderService _orderService;
    private readonly ITranslationService _translationService;

    public HadSpentAmountDiscountRule(IOrderService orderService, ITranslationService translationService)
    {
        _orderService = orderService;
        _translationService = translationService;
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

        if (!double.TryParse(request.DiscountRule.Metadata, out var spentAmountRequirement)) return result;

        if (spentAmountRequirement == 0)
        {
            //valid
            result.IsValid = true;
            return result;
        }

        if (request.Customer == null)
            return result;

        var orders = await _orderService.SearchOrders(request.Store.Id,
            customerId: request.Customer.Id,
            os: (int)OrderStatusSystem.Complete);
        var spentAmount = orders.Sum(o => o.OrderTotal);
        if (spentAmount > spentAmountRequirement)
            result.IsValid = true;
        else
            result.UserError =
                _translationService.GetResource("Plugins.DiscountRules.Standard.HadSpentAmount.NotEnough");
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
        var result = "Admin/HadSpentAmount/Configure/?discountId=" + discountId;
        if (!string.IsNullOrEmpty(discountRequirementId))
            result += $"&discountRequirementId={discountRequirementId}";
        return result;
    }

    public string FriendlyName => "Customer had spent x.xx amount";

    public string SystemName => "DiscountRules.Standard.HadSpentAmount";
}