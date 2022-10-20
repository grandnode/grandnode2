using FluentValidation;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Domain.Orders;

namespace Grand.Business.Checkout.Validators
{

    public record ShoppingCartReservationProductValidatorRecord(Customer Customer, Product Product, ShoppingCartItem ShoppingCartItem);

    public class ShoppingCartReservationProductValidator : AbstractValidator<ShoppingCartReservationProductValidatorRecord>
    {
        public ShoppingCartReservationProductValidator(ITranslationService translationService, IProductReservationService productReservationService)
        {

            RuleFor(x => x).CustomAsync(async (value, context, ct) =>
            {
                if (!string.IsNullOrEmpty(value.ShoppingCartItem.ReservationId))
                {
                    var reservations = await productReservationService.GetCustomerReservationsHelpers(value.Customer.Id);
                    if (!value.Customer.ShoppingCartItems.Any(x => x.Id == value.ShoppingCartItem.Id))
                    {
                        if (reservations.Where(x => x.ReservationId == value.ShoppingCartItem.ReservationId).Any())
                            context.AddFailure(translationService.GetResource("ShoppingCart.AlreadyReservation"));
                    }
                }

                if (value.ShoppingCartItem.RentalStartDateUtc.HasValue && value.ShoppingCartItem.RentalEndDateUtc.HasValue)
                {
                    var _canBeBook = false;
                    var reservations = await productReservationService.GetProductReservationsByProductId(value.Product.Id, true, null);
                    var reserved = await productReservationService.GetCustomerReservationsHelpers(value.Customer.Id);
                    foreach (var item in reserved.Where(x => x.ShoppingCartItemId != value.ShoppingCartItem.Id))
                    {
                        var match = reservations.Where(x => x.Id == item.ReservationId).FirstOrDefault();
                        if (match != null)
                        {
                            reservations.Remove(match);
                        }
                    }

                    var grouped = reservations.GroupBy(x => x.Resource);
                    foreach (var group in grouped)
                    {
                        bool groupCanBeBooked = true;
                        if (value.Product.IncBothDate && value.Product.IntervalUnitId == IntervalUnit.Day)
                        {
                            for (DateTime iterator = value.ShoppingCartItem.RentalStartDateUtc.Value; iterator <= value.ShoppingCartItem.RentalEndDateUtc.Value; iterator += new TimeSpan(24, 0, 0))
                            {
                                if (!group.Select(x => x.Date).Contains(iterator))
                                {
                                    groupCanBeBooked = false;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            for (DateTime iterator = value.ShoppingCartItem.RentalStartDateUtc.Value; iterator < value.ShoppingCartItem.RentalEndDateUtc.Value; iterator += new TimeSpan(24, 0, 0))
                            {
                                if (!group.Select(x => x.Date).Contains(iterator))
                                {
                                    groupCanBeBooked = false;
                                    break;
                                }
                            }
                        }
                        if (groupCanBeBooked)
                        {
                            _canBeBook = true;
                            break;
                        }
                    }

                    if (!_canBeBook)
                    {
                        context.AddFailure(translationService.GetResource("ShoppingCart.Reservation.NoFreeReservationsInThisPeriod"));
                        return;
                    }
                }

                if (string.IsNullOrEmpty(value.ShoppingCartItem.ReservationId) && value.Product.IntervalUnitId != IntervalUnit.Day)
                {
                    context.AddFailure(translationService.GetResource("ShoppingCart.Reservation.NoReservationFound"));
                    return;
                }

                if (value.Product.IntervalUnitId != IntervalUnit.Day)
                {
                    var reservation = await productReservationService.GetProductReservation(value.ShoppingCartItem.ReservationId);
                    if (reservation == null)
                    {
                        context.AddFailure(translationService.GetResource("ShoppingCart.Reservation.ReservationDeleted"));
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(reservation.OrderId))
                        {
                            context.AddFailure(translationService.GetResource("ShoppingCart.Reservation.AlreadyReserved"));
                        }
                    }
                }
                else
                {
                    if (!(value.ShoppingCartItem.RentalStartDateUtc.HasValue && value.ShoppingCartItem.RentalEndDateUtc.HasValue))
                    {
                        context.AddFailure(translationService.GetResource("ShoppingCart.Reservation.ChoosebothDates"));
                    }
                    else
                    {
                        if (!value.Product.IncBothDate)
                        {
                            if (value.ShoppingCartItem.RentalStartDateUtc.Value >= value.ShoppingCartItem.RentalEndDateUtc.Value)
                            {
                                context.AddFailure(translationService.GetResource("ShoppingCart.Reservation.EndDateMustBeLaterThanStartDate"));
                            }
                        }
                        else
                        {
                            if (value.ShoppingCartItem.RentalStartDateUtc.Value > value.ShoppingCartItem.RentalEndDateUtc.Value)
                            {
                                context.AddFailure(translationService.GetResource("ShoppingCart.Reservation.EndDateMustBeLaterThanStartDate"));
                            }
                        }

                        if (value.ShoppingCartItem.RentalStartDateUtc.Value < DateTime.Now || value.ShoppingCartItem.RentalEndDateUtc.Value < DateTime.Now)
                        {
                            context.AddFailure(translationService.GetResource("ShoppingCart.Reservation.ReservationDatesMustBeLaterThanToday"));
                        }

                        if (value.Customer.ShoppingCartItems.Any(x => x.Id == value.ShoppingCartItem.Id))
                        {
                            var reserved = await productReservationService.GetCustomerReservationsHelperBySciId(value.ShoppingCartItem.Id);
                            if (!reserved.Any())
                                context.AddFailure(translationService.GetResource("ShoppingCart.Reservation.ReservationNotExists"));
                            else
                                foreach (var item in reserved)
                                {
                                    var reservation = await productReservationService.GetProductReservation(item.ReservationId);
                                    if (reservation == null)
                                    {
                                        context.AddFailure(translationService.GetResource("ShoppingCart.Reservation.ReservationDeleted"));
                                        break;
                                    }
                                    else if (!string.IsNullOrEmpty(reservation.OrderId))
                                    {
                                        context.AddFailure(translationService.GetResource("ShoppingCart.Reservation.AlreadyReserved"));
                                        break;
                                    }
                                }
                        }
                    }
                }
            });
        }
    }
}
