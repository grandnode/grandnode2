using Grand.Business.System.Interfaces.Installation;
using Grand.Domain.Shipping;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService : IInstallationService
    {
        protected virtual async Task InstallShippingMethods()
        {
            var shippingMethods = new List<ShippingMethod>
                                {
                                    new ShippingMethod
                                        {
                                            Name = "Ground",
                                            Description ="Compared to other shipping methods, ground shipping is carried out closer to the earth",
                                            DisplayOrder = 1
                                        },
                                    new ShippingMethod
                                        {
                                            Name = "Next Day Air",
                                            Description ="The one day air shipping",
                                            DisplayOrder = 3
                                        },
                                    new ShippingMethod
                                        {
                                            Name = "2nd Day Air",
                                            Description ="The two day air shipping",
                                            DisplayOrder = 3
                                        }
                                };
            await _shippingMethodRepository.InsertAsync(shippingMethods);
        }
    }
}
