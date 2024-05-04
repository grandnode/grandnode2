using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Customers;
using Grand.Domain.Orders;

namespace Grand.Web.Extensions;

public static class OrderExtensions
{
    public static async Task<bool> Access(this Order order, Customer customer, IGroupService groupService)
    {
        if (order == null || order.Deleted)
            return false;

        //owner
        if (await groupService.IsOwner(customer) && (customer.Id == order.CustomerId || customer.Id == order.OwnerId))
            return true;

        //sub account
        return !await groupService.IsOwner(customer) && customer.Id == order.CustomerId;
    }

    public static async Task<bool> Access(this MerchandiseReturn merchandiseReturn, Customer customer,
        IGroupService groupService)
    {
        if (merchandiseReturn == null)
            return false;

        //owner
        if (await groupService.IsOwner(customer) &&
            (customer.Id == merchandiseReturn.CustomerId || customer.Id == merchandiseReturn.OwnerId))
            return true;

        //sub account
        return !await groupService.IsOwner(customer) && customer.Id == merchandiseReturn.CustomerId;
    }
}