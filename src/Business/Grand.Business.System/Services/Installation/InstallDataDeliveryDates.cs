using Grand.Business.System.Interfaces.Installation;
using Grand.Domain.Shipping;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService : IInstallationService
    {
        protected virtual async Task InstallDeliveryDates()
        {
            var deliveryDates = new List<DeliveryDate>
                                {
                                    new DeliveryDate
                                        {
                                            Name = "1-2 days",
                                            DisplayOrder = 1
                                        },
                                    new DeliveryDate
                                        {
                                            Name = "3-5 days",
                                            DisplayOrder = 5
                                        },
                                    new DeliveryDate
                                        {
                                            Name = "1 week",
                                            DisplayOrder = 10
                                        },
                                };
            await _deliveryDateRepository.InsertAsync(deliveryDates);
        }
    }
}
