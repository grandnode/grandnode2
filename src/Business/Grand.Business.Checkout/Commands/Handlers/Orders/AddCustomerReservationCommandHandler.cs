using Grand.Business.Core.Commands.Checkout.Orders;
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

            IGrouping<string, ProductReservation> groupToBook = null;

            var grouped = reservations.GroupBy(x => x.Resource);
            foreach (var group in grouped)
            {
                var groupCanBeBooked = true;
                if (request.Product.IncBothDate && request.Product.IntervalUnitId == IntervalUnit.Day)
                    for (var iterator = request.RentalStartDate.Value;
                         iterator <= request.RentalEndDate.Value;
                         iterator += new TimeSpan(24, 0, 0))
                    {
                        if (group.Select(x => x.Date).Contains(iterator)) continue;
                        groupCanBeBooked = false;
                        break;
                    }
                else
                    for (var iterator = request.RentalStartDate.Value;
                         iterator < request.RentalEndDate.Value;
                         iterator += new TimeSpan(24, 0, 0))
                    {
                        if (group.Select(x => x.Date).Contains(iterator)) continue;
                        groupCanBeBooked = false;
                        break;
                    }

                if (!groupCanBeBooked) continue;
                groupToBook = group;
                break;
            }

            if (groupToBook != null)
            {
                if (request.Product.IncBothDate && request.Product.IntervalUnitId == IntervalUnit.Day)
                    foreach (var item in groupToBook.Where(x =>
                                 x.Date >= request.RentalStartDate && x.Date <= request.RentalEndDate))
                        await _productReservationService.InsertCustomerReservationsHelper(new CustomerReservationsHelper {
                            CustomerId = request.Customer.Id,
                            ReservationId = item.Id,
                            ShoppingCartItemId = request.ShoppingCartItem.Id
                        });
                else
                    foreach (var item in groupToBook.Where(x =>
                                 x.Date >= request.RentalStartDate && x.Date < request.RentalEndDate))
                        await _productReservationService.InsertCustomerReservationsHelper(new CustomerReservationsHelper {
                            CustomerId = request.Customer.Id,
                            ReservationId = item.Id,
                            ShoppingCartItemId = request.ShoppingCartItem.Id
                        });
            }
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