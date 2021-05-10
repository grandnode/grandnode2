using DiscountRules.Standard.Models;
using Grand.Business.Catalog.Interfaces.Discounts;
using Grand.Business.Catalog.Utilities;
using Grand.Business.Checkout.Extensions;
using Grand.Business.Common.Interfaces.Configuration;
using Grand.Domain.Orders;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DiscountRules.Provider
{
    public partial class HasAllProductsDiscountRule : IDiscountRule
    {
        private readonly ISettingService _settingService;
        private readonly ShoppingCartSettings _shoppingCartSettings;

        public HasAllProductsDiscountRule(ISettingService settingService, ShoppingCartSettings shoppingCartSettings)
        {
            _settingService = settingService;
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

            //invalid by default
            var result = new DiscountRuleValidationResult();

            var restrictedProductIds = _settingService.GetSettingByKey<RequirementProducts>(string.Format("DiscountRules.Standard.RestrictedProductIds-{0}-{1}", request.DiscountId, request.DiscountRequirementId));

            if (restrictedProductIds == null || !restrictedProductIds.Products.Any())
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
            var cartQuery = from sci in request.Customer.ShoppingCartItems.LimitPerStore(_shoppingCartSettings.SharedCartBetweenStores, request.Store.Id)
                            where sci.ShoppingCartTypeId == ShoppingCartType.ShoppingCart
                            group sci by sci.ProductId into g
                            select new { ProductId = g.Key, TotalQuantity = g.Sum(x => x.Quantity) };
            var cart = cartQuery.ToList();

            bool allFound = true;
            foreach (var restrictedProduct in restrictedProductIds.Products.Select(x => x.Trim()))
            {
                if (String.IsNullOrWhiteSpace(restrictedProduct))
                    continue;

                bool found1 = false;
                foreach (var sci in cart)
                {
                    if (restrictedProduct.Contains(":"))
                    {
                        if (restrictedProduct.Contains("-"))
                        {
                            //the third way (the quantity rage specified)
                            //{Product ID}:{Min quantity}-{Max quantity}. For example, 77:1-3, 123:2-5, 156:3-8
                            string restrictedProductId = restrictedProduct.Split(new[] { ':' })[0];
                            int quantityMin;
                            if (!int.TryParse(restrictedProduct.Split(new[] { ':' })[1].Split(new[] { '-' })[0], out quantityMin))
                                //parsing error; exit;
                                return result;
                            int quantityMax;
                            if (!int.TryParse(restrictedProduct.Split(new[] { ':' })[1].Split(new[] { '-' })[1], out quantityMax))
                                //parsing error; exit;
                                return result;

                            if (sci.ProductId == restrictedProductId && quantityMin <= sci.TotalQuantity && sci.TotalQuantity <= quantityMax)
                            {
                                found1 = true;
                                break;
                            }
                        }
                        else
                        {
                            //the second way (the quantity specified)
                            //{Product ID}:{Quantity}. For example, 77:1, 123:2, 156:3
                            string restrictedProductId = restrictedProduct.Split(new[] { ':' })[0];
                            int quantity;
                            if (!int.TryParse(restrictedProduct.Split(new[] { ':' })[1], out quantity))
                                //parsing error; exit;
                                return result;

                            if (sci.ProductId == restrictedProductId && sci.TotalQuantity == quantity)
                            {
                                found1 = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        //the first way (the quantity is not specified)
                        if (sci.ProductId == restrictedProduct)
                        {
                            found1 = true;
                            break;
                        }
                    }
                }

                if (!found1)
                {
                    allFound = false;
                    break;
                }
            }

            if (allFound)
            {
                //valid
                result.IsValid = true;
                return result;
            }

            return await Task.FromResult(result);
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
            string result = "Admin/HasAllProducts/Configure/?discountId=" + discountId;
            if (!String.IsNullOrEmpty(discountRequirementId))
                result += string.Format("&discountRequirementId={0}", discountRequirementId);
            return result;
        }

        public string FriendlyName => "Customer has all of these products in";

        public string SystemName => "DiscountRules.HasAllProducts";
    }
}