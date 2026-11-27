using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Payments;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;

namespace Grand.Web.AdminShared.Services;

/// <summary>
///     Admin's <see cref="IAdminDataScope{PaymentTransaction}" />: a sales manager reaches the payments of their own
///     orders only, as <see cref="AdminOrderDataScope" /> narrows the orders. The sales employee is on the order, not on
///     the payment.
/// </summary>
public class AdminPaymentTransactionDataScope(
    IContextAccessor contextAccessor,
    IGroupService groupService,
    IOrderService orderService) : IAdminDataScope<PaymentTransaction>
{
    public async Task<bool> HasAccess(PaymentTransaction entity)
    {
        if (entity is null) return false;
        var customer = contextAccessor.WorkContext.CurrentCustomer;
        if (!await groupService.IsSalesManager(customer)) return true;

        var order = await orderService.GetOrderByGuid(entity.OrderGuid);
        return order is not null && order.SeId == customer.SeId;
    }

    public string? DefaultStoreId => null;
    public string ResourceKeyPrefix => "Admin";
    public bool ShowStoreSelector => true;
    public string? DefaultVendorId => null;
    public bool CanFeatureOnHomepage => true; // unused for PaymentTransaction; required interface member
}