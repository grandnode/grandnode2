using Grand.Business.System.Interfaces.Installation;
using Grand.Domain.Customers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService : IInstallationService
    {
        protected virtual async Task InstallCustomerAction()
        {
            var customerActionType = new List<CustomerActionType>()
            {
                new CustomerActionType()
                {
                    Name = "Add to cart",
                    SystemKeyword = "AddToCart",
                    Enabled = false,
                    ConditionType = {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 13 }
                },
                new CustomerActionType()
                {
                    Name = "Add order",
                    SystemKeyword = "AddOrder",
                    Enabled = false,
                    ConditionType = {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 13 }
                },
                new CustomerActionType()
                {
                    Name = "Paid order",
                    SystemKeyword = "PaidOrder",
                    Enabled = false,
                    ConditionType = {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 13 }
                },
                new CustomerActionType()
                {
                    Name = "Viewed",
                    SystemKeyword = "Viewed",
                    Enabled = false,
                    ConditionType = {1, 2, 3, 7, 8, 9, 10, 13}
                },
                new CustomerActionType()
                {
                    Name = "Url",
                    SystemKeyword = "Url",
                    Enabled = false,
                    ConditionType = {7, 8, 9, 10, 11, 12, 13}
                },
                new CustomerActionType()
                {
                    Name = "Customer Registration",
                    SystemKeyword = "Registration",
                    Enabled = false,
                    ConditionType = {7, 8, 9, 10, 13}
                }
            };
            await _customerActionType.InsertAsync(customerActionType);

        }
    }
}
