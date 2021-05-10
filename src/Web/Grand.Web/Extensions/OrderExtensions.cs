using Grand.Business.Common.Interfaces.Directory;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using System.Threading.Tasks;

namespace Grand.Web.Extensions
{
    public static class OrderExtensions
    {
        public static async Task<bool> Access(this Order order, Customer customer, IGroupService groupService)
        {
            if (order == null || order.Deleted)
                return false;

            //owner
            if (await groupService.IsOwner(customer) && (customer.Id == order.CustomerId || customer.Id == order.OwnerId))
                return true;

            //subaccount
            if (!await groupService.IsOwner(customer) && customer.Id == order.CustomerId)
                return true;

            return false;
        }
        public static async Task<bool> Access(this MerchandiseReturn merchandiseReturn, Customer customer, IGroupService groupService)
        {
            if (merchandiseReturn == null)
                return false;

            //owner
            if (await groupService.IsOwner(customer) && (customer.Id == merchandiseReturn.CustomerId || customer.Id == merchandiseReturn.OwnerId))
                return true;

            //subaccount
            if (!await groupService.IsOwner(customer) && customer.Id == merchandiseReturn.CustomerId)
                return true;

            return false;
        }
    }
}
