using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using MediatR;

namespace Grand.Business.Checkout.Commands.Handlers.Orders;

public class AddCustomerReservationCommandHandler : IRequestHandler<AddCustomerReservationCommand, bool>
{
    private readonly IProductReservationService _productReservationService;

    public AddCustomerReservationCommandHandler(IProductReservationService productReservationService)
    {
        _productReservationService = productReservationService;
    }

    public async Task<bool> Handle(AddCustomerReservationCommand request, CancellationToken cancellationToken)
    {
        if (request.RentalStartDate.HasValue && request.RentalEndDate.HasValue)
        {
            var reservations =
                await _productReservationService.GetProductReservationsByProductId(request.Product.Id, true, null);
            var reserved = await _productReservationService.GetCustomerReservationsHelpers(request.Customer.Id);
            foreach (var item in reserved)
            {
                var match = reservations.FirstOrDefault(x => x.Id == item.ReservationId);
                if (match != null) reservations.Remove(match);
            }

            var groupToBook = reservations.FindGroupToBook(request.Product, request.RentalStartDate.Value,
                request.RentalEndDate.Value);

            if (groupToBook != null)
                foreach (var item in groupToBook.InRentalPeriod(request.Product, request.RentalStartDate.Value,
                             request.RentalEndDate.Value))
                    await _productReservationService.InsertCustomerReservationsHelper(new CustomerReservationsHelper {
                        CustomerId = request.Customer.Id,
                        ReservationId = item.Id,
                        ShoppingCartItemId = request.ShoppingCartItem.Id
                    });
        }

        if (!string.IsNullOrEmpty(request.ReservationId))
            await _productReservationService.InsertCustomerReservationsHelper(new CustomerReservationsHelper {
                CustomerId = request.Customer.Id,
                ReservationId = request.ReservationId,
                ShoppingCartItemId = request.ShoppingCartItem.Id
            });
        return true;
    }
}