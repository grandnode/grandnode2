using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Mediator;
using Grand.Web.Commands.Models.Orders;
using Grand.Web.Extensions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

namespace Grand.Web.Commands.Handler.Orders;

public class SendGuestMerchandiseReturnLinkCommandHandler(
    IOrderService orderService,
    ICustomerService customerService,
    IGroupService groupService,
    IMerchandiseReturnMessageProviderService merchandiseReturnMessageProviderService,
    IDataProtectionProvider dataProtectionProvider,
    ICacheBase cacheBase,
    ILogger<SendGuestMerchandiseReturnLinkCommandHandler> logger)
    : IRequestHandler<SendGuestMerchandiseReturnLinkCommand, bool>
{
    /// <summary>
    ///     At most one link per order in this time: asking again does not flood the order's mailbox
    /// </summary>
    private const int ThrottleMinutes = 5;

    public async Task<bool> Handle(SendGuestMerchandiseReturnLinkCommand request, CancellationToken cancellationToken)
    {
        var order = await orderService.GetOrderByNumber(request.OrderNumber);
        if (order == null || order.StoreId != request.Store.Id)
            return false;

        //the address the order was placed with is the proof that it is theirs; a guest's is kept in the billing
        //address only, where the order's e-mails go
        var orderEmail = string.IsNullOrEmpty(order.CustomerEmail) ? order.BillingAddress?.Email : order.CustomerEmail;
        if (!string.Equals(orderEmail?.Trim(), request.Email?.Trim(), StringComparison.OrdinalIgnoreCase))
            return false;

        if (await MerchandiseReturnGuestGrant.GuestOfOrder(order, request.Store.Id, customerService,
                groupService) == null)
            return false;

        var first = false;
        await cacheBase.GetAsync(string.Format(CacheKey.MERCHANDISE_RETURN_GUEST_LINK_KEY, order.Id), () =>
        {
            first = true;
            return Task.FromResult(true);
        }, ThrottleMinutes);
        if (!first)
            return false;

        try
        {
            return await merchandiseReturnMessageProviderService.SendMerchandiseReturnGuestLinkCustomerMessage(order,
                MerchandiseReturnGuestGrant.LinkToken(dataProtectionProvider, order), request.Host) > 0;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            //only a matching order gets this far: an error page would tell that it exists
            logger.LogError(ex, "The return link of order {OrderId} was not sent", order.Id);
            return false;
        }
    }
}
