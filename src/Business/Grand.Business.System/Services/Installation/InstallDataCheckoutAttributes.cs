using Grand.Business.Core.Interfaces.System.Installation;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService : IInstallationService
    {
        protected virtual async Task InstallCheckoutAttributes()
        {
            var ca1 = new CheckoutAttribute
            {
                Name = "Gift wrapping",
                IsRequired = true,
                ShippableProductRequired = true,
                AttributeControlTypeId = AttributeControlType.DropdownList,
                DisplayOrder = 1,
            };
            await _checkoutAttributeRepository.InsertAsync(ca1);
            ca1.CheckoutAttributeValues.Add(new CheckoutAttributeValue
            {
                Name = "No",
                PriceAdjustment = 0,
                DisplayOrder = 1,
                IsPreSelected = true,
                CheckoutAttributeId = ca1.Id,
            });

            ca1.CheckoutAttributeValues.Add(new CheckoutAttributeValue
            {
                Name = "Yes",
                PriceAdjustment = 10,
                DisplayOrder = 2,
                CheckoutAttributeId = ca1.Id,
            });
            await _checkoutAttributeRepository.UpdateAsync(ca1);
        }
    }
}
