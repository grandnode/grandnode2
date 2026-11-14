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

public class SendGuestOrderLinkCommandHandler(
    IOrderService orderService,
    ICustomerService customerService,
    IGroupService groupService,
    IOrderMessageProviderService orderMessageProviderService,
    IDataProtectionProvider dataProtectionProvider,
    ICacheBase cacheBase,
    ILogger<SendGuestOrderLinkCommandHandler> logger)
    : IRequestHandler<SendGuestOrderLinkCommand, bool>
{
    /// <summary>
    ///     At most one link per order in this time: asking again does not flood the order's mailbox
    /// </summary>
    private const int ThrottleMinutes = 5;

    public async Task<bool> Handle(SendGuestOrderLinkCommand request, CancellationToken cancellationToken)
    {
        //the number every order e-mail shows, or the code the confirmation shows since 2.5; a code is not unique,
        //so the address the order was placed with, the proof that it is theirs, picks the order
        var value = request.OrderNumberOrCode?.Trim();
        var candidates = (await orderService.GetOrdersByCode(value)).ToList();
        if (int.TryParse(value, out var number) && number > 0 &&
            await orderService.GetOrderByNumber(number) is { } byNumber)
            candidates.Add(byNumber);

        var order = candidates.FirstOrDefault(x => x.StoreId == request.Store.Id && EmailMatches(x, request.Email));
        if (order == null)
            return false;

        if (await GuestOrderGrant.GuestOfOrder(order, request.Store.Id, customerService,
                groupService) == null)
            return false;

        var throttleKey = string.Format(CacheKey.GUEST_ORDER_LINK_KEY, order.Id);
        var first = false;
        await cacheBase.GetAsync(throttleKey, () =>
        {
            first = true;
            return Task.FromResult(true);
        }, ThrottleMinutes);
        if (!first)
            return false;

        var sent = false;
        try
        {
            sent = await orderMessageProviderService.SendGuestOrderLinkCustomerMessage(order,
                GuestOrderGrant.LinkToken(dataProtectionProvider, order), request.Host) > 0;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            //only a matching order gets this far: an error page would tell that it exists
            logger.LogError(ex, "The guest link of order {OrderId} was not sent", order.Id);
        }
        finally
        {
            //a link that was not sent leaves the guest free to ask again
            if (!sent)
                await cacheBase.RemoveAsync(throttleKey);
        }

        return sent;
    }

    /// <summary>
    ///     Whether the address is the one the order was placed with: a guest's is kept in the billing address only,
    ///     where the order's e-mails go
    /// </summary>
    private static bool EmailMatches(Domain.Orders.Order order, string email)
    {
        var orderEmail = string.IsNullOrEmpty(order.CustomerEmail) ? order.BillingAddress?.Email : order.CustomerEmail;
        return !string.IsNullOrEmpty(orderEmail) &&
               string.Equals(orderEmail.Trim(), email?.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}
