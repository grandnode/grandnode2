using Grand.Business.System.Interfaces.Installation;
using Grand.Domain.Discounts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService : IInstallationService
    {
        protected virtual async Task InstallDiscounts()
        {
            var discounts = new List<Discount>
                                {
                                    new Discount
                                        {
                                            Name = "Sample discount with coupon code",
                                            DiscountTypeId = DiscountType.AssignedToOrderTotal,
                                            DiscountLimitationId = DiscountLimitationType.Nolimits,
                                            UsePercentage = false,
                                            DiscountAmount = 10,
                                            RequiresCouponCode = true,
                                            IsEnabled = true,
                                            CurrencyCode = "USD"
                                        },
                                    new Discount
                                        {
                                            Name = "'20% order total' discount",
                                            DiscountTypeId = DiscountType.AssignedToOrderTotal,
                                            DiscountLimitationId = DiscountLimitationType.Nolimits,
                                            UsePercentage = true,
                                            DiscountPercentage = 20,
                                            StartDateUtc = new DateTime(2010,1,1),
                                            EndDateUtc = new DateTime(2030,1,1),
                                            RequiresCouponCode = true,
                                            IsEnabled = true,
                                            CurrencyCode = "USD"
                                        },
                                };
            await _discountRepository.InsertAsync(discounts);
            var coupon1 = new DiscountCoupon
            {
                CouponCode = "123",
                DiscountId = _discountRepository.Table.Where(x => x.Name == "Sample discount with coupon code").FirstOrDefault().Id
            };
            await _discountCouponRepository.InsertAsync(coupon1);

            var coupon2 = new DiscountCoupon
            {
                CouponCode = "456",
                DiscountId = _discountRepository.Table.Where(x => x.Name == "'20% order total' discount").FirstOrDefault().Id
            };
            await _discountCouponRepository.InsertAsync(coupon2);
        }
    }
}
