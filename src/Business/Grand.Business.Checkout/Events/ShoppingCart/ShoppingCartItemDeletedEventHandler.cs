using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Orders;
using Grand.Infrastructure.Events;
using MediatR;

namespace Grand.Business.Checkout.Events.ShoppingCart;

/// <summary>
///     Update order items
/// </summary>
public class ShoppingCartItemDeletedEventHandler : INotificationHandler<EntityDeleted<ShoppingCartItem>>
{
    private readonly IProductReservationService _productReservationService;

    public ShoppingCartItemDeletedEventHandler(IProductReservationService productReservationService)
    {
        _productReservationService = productReservationService;
    }

    public async Task Handle(EntityDeleted<ShoppingCartItem> notification, CancellationToken cancellationToken)
    {
        if ((notification.Entity.RentalStartDateUtc.HasValue && notification.Entity.RentalEndDateUtc.HasValue) ||
            !string.IsNullOrEmpty(notification.Entity.ReservationId))
        {
            var reserved =
                await _productReservationService.GetCustomerReservationsHelperBySciId(notification.Entity.Id);
            foreach (var res in reserved) await _productReservationService.DeleteCustomerReservationsHelper(res);
        }
    }
}