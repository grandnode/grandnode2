using DiscountRules.Standard.Models;
using Grand.Business.Catalog.Interfaces.Discounts;
using Grand.Business.Catalog.Interfaces.Prices;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Catalog.Utilities;
using Grand.Business.Checkout.Extensions;
using Grand.Business.Common.Interfaces.Configuration;
using Grand.Domain.Orders;
using Grand.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DiscountRules.Provider
{
    public partial class ShoppingCartDiscountRule : IDiscountRule
    {
        private readonly IWorkContext _workContext;
        private readonly IProductService _productService;
        private readonly ISettingService _settingService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        public ShoppingCartDiscountRule(
            IWorkContext workContext, 
            IProductService productService,
            IServiceProvider serviceProvider,
            ISettingService settingService, 
            ShoppingCartSettings shoppingCartSettings)
        {
            _workContext = workContext;
            _productService = productService;
            _settingService = settingService;
            _serviceProvider = serviceProvider;
            _shoppingCartSettings = shoppingCartSettings;
        }

        /// <summary>
        /// Check discount requirement
        /// </summary>
        /// <param name="request">Object that contains all information required to check the requirement (Current customer, discount, etc)</param>
        /// <returns>true - requirement is met; otherwise, false</returns>
        public async Task<DiscountRuleValidationResult> CheckRequirement(DiscountRuleValidationRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var result = new DiscountRuleValidationResult();

            var spentAmountRequirement = _settingService.GetSettingByKey<RequirementSpentAmount>(string.Format("DiscountRequirement.ShoppingCart-{0}", request.DiscountRequirementId));

            if (spentAmountRequirement == null || spentAmountRequirement.SpentAmount == 0)
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

            var priceCalculationService = _serviceProvider.GetRequiredService<IPricingService>();

            foreach (var ca in cart)
            {
                bool calculateWithDiscount = false;
                var product = await _productService.GetProductById(ca.ProductId);
                if (product != null)
                    spentAmount += (await priceCalculationService.GetSubTotal(ca, product, calculateWithDiscount)).subTotal;
            }

            result.IsValid = spentAmount > spentAmountRequirement.SpentAmount;
            return result;
        }

        /// <summary>
        /// Get URL for rule configuration
        /// </summary>
        /// <param name="discountId">Discount identifier</param>
        /// <param name="discountRequirementId">Discount requirement identifier (if editing)</param>
        /// <returns>URL</returns>
        public string GetConfigurationUrl(string discountId, string discountRequirementId)
        {
            //configured 
            string result = "Admin/ShoppingCartAmount/Configure/?discountId=" + discountId;
            if (!String.IsNullOrEmpty(discountRequirementId))
                result += string.Format("&discountRequirementId={0}", discountRequirementId);
            return result;
        }
        public string FriendlyName => "SubTotal in Shopping Cart x.xx ";
        public string SystemName => "DiscountRequirement.ShoppingCart";

    }
}