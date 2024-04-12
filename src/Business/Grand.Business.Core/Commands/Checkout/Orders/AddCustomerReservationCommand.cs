using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using MediatR;

namespace Grand.Business.Core.Commands.Checkout.Orders;

public class AddCustomerReservationCommand : IRequest<bool>
{
    public Customer Customer { get; set; }
    public Product Product { get; set; }
    public DateTime? RentalStartDate { get; set; }
    public DateTime? RentalEndDate { get; set; }
    public ShoppingCartItem ShoppingCartItem { get; set; }
    public string ReservationId { get; set; }
}