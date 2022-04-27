using Grand.Business.Core.Interfaces.System.Installation;
using Grand.Domain.Orders;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService : IInstallationService
    {
        protected virtual async Task InstallMerchandiseReturnActions()
        {
            var merchandiseReturnActions = new List<MerchandiseReturnAction>
                                {
                                    new MerchandiseReturnAction
                                        {
                                            Name = "Repair",
                                            DisplayOrder = 1
                                        },
                                    new MerchandiseReturnAction
                                        {
                                            Name = "Replacement",
                                            DisplayOrder = 2
                                        },
                                    new MerchandiseReturnAction
                                        {
                                            Name = "Store Credit",
                                            DisplayOrder = 3
                                        }
                                };
            await _merchandiseReturnActionRepository.InsertAsync(merchandiseReturnActions);
        }

    }
}
