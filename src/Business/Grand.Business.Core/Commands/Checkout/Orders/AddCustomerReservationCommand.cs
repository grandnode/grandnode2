using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Mediator;

namespace Grand.Business.Core.Commands.Checkout.Orders;

public class AddCustomerReservationCommand : IRequest<bool>
{
    public required Customer Customer { get; set; }
    public required Product Product { get; set; }
    public DateTime? RentalStartDate { get; set; }
    public DateTime? RentalEndDate { get; set; }
    public required ShoppingCartItem ShoppingCartItem { get; set; }
    public string? ReservationId { get; set; }
}