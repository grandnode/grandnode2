using Grand.Domain.Customers;
using Grand.Domain.Discounts;

namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    protected virtual async Task InstallDiscounts()
    {
        var registeredGroupId = _customerGroupRepository.Table
            .Single(x => x.SystemName == SystemCustomerGroupNames.Registered).Id;

        var welcome10 = new Discount {
            Name = "Welcome 10 percent",
            DiscountTypeId = DiscountType.AssignedToOrderTotal,
            DiscountLimitationId = DiscountLimitationType.NTimesPerUser,
            LimitationTimes = 1,
            UsePercentage = true,
            DiscountPercentage = 10,
            RequiresCouponCode = true,
            IsEnabled = true,
            CurrencyCode = "USD"
        };

        var outdoorAndActive15 = new Discount {
            Name = "Outdoor and Active 15 percent",
            DiscountTypeId = DiscountType.AssignedToCategories,
            DiscountLimitationId = DiscountLimitationType.Nolimits,
            UsePercentage = true,
            DiscountPercentage = 15,
            IsEnabled = true,
            CurrencyCode = "USD"
        };

        var coffeeBundle20 = new Discount {
            Name = "Coffee Bundle 20 percent",
            DiscountTypeId = DiscountType.AssignedToSkus,
            DiscountLimitationId = DiscountLimitationType.Nolimits,
            UsePercentage = true,
            DiscountPercentage = 20,
            IsEnabled = true,
            CurrencyCode = "USD"
        };

        var freeShippingOver75 = new Discount {
            Name = "Free shipping over 75",
            DiscountTypeId = DiscountType.AssignedToShipping,
            DiscountLimitationId = DiscountLimitationType.Nolimits,
            UsePercentage = true,
            DiscountPercentage = 100,
            IsEnabled = true,
            CurrencyCode = "USD"
        };
        freeShippingOver75.DiscountRules.Add(new DiscountRule {
            DiscountRequirementRuleSystemName = "DiscountRequirement.ShoppingCart",
            Metadata = "75"
        });

        var members5 = new Discount {
            Name = "Members 5 percent",
            DiscountTypeId = DiscountType.AssignedToOrderSubTotal,
            DiscountLimitationId = DiscountLimitationType.Nolimits,
            UsePercentage = true,
            DiscountPercentage = 5,
            IsEnabled = true,
            CurrencyCode = "USD"
        };
        members5.DiscountRules.Add(new DiscountRule {
            DiscountRequirementRuleSystemName = "DiscountRules.Standard.MustBeAssignedToCustomerGroup",
            Metadata = registeredGroupId
        });

        var discounts = new List<Discount> {
            welcome10,
            outdoorAndActive15,
            coffeeBundle20,
            freeShippingOver75,
            members5
        };
        discounts.ForEach(x => _discountRepository.Insert(x));

        //category target - Outdoor & Active; category discounts apply only to a product's own
        //categories, and products sit in the subcategories, so the discount goes on all of them
        foreach (var categoryId in new[] {
                     CategoryId("Outdoor & Active"), CategoryId("Camping"), CategoryId("Cycling"), CategoryId("Running")
                 })
        {
            var category = _categoryRepository.Table.Single(x => x.Id == categoryId);
            category.AppliedDiscounts.Add(outdoorAndActive15.Id);
            await _categoryRepository.UpdateAsync(category);
        }

        //product target - Slow Morning Coffee Bundle
        var coffeeBundleProduct = ProductByName("Slow Morning Coffee Bundle");
        coffeeBundleProduct.AppliedDiscounts.Add(coffeeBundle20.Id);
        await _productRepository.UpdateAsync(coffeeBundleProduct);

        var welcome10Coupon = new DiscountCoupon {
            CouponCode = "WELCOME10",
            DiscountId = welcome10.Id
        };
        await _discountCouponRepository.InsertAsync(welcome10Coupon);
    }
}
