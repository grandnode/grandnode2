using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Customers.Interfaces;
using Grand.Domain.Catalog;
using Grand.Domain.Data;
using Grand.Domain.Orders;
using MediatR;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Events.Orders
{
    public class OrderPlacedEventHandler : INotificationHandler<OrderPlacedEvent>
    {
        private readonly IRepository<ProductAlsoPurchased> _productAlsoPurchasedRepository;
        private readonly ICustomerService _customerService;
        private readonly ILoyaltyPointsService _loyaltyPointsService;

        public OrderPlacedEventHandler(
            IRepository<ProductAlsoPurchased> productAlsoPurchasedRepository,
            ICustomerService customerService,
            ILoyaltyPointsService loyaltyPointsService)
        {
            _productAlsoPurchasedRepository = productAlsoPurchasedRepository;
            _customerService = customerService;
            _loyaltyPointsService = loyaltyPointsService;
        }

        public Task Handle(OrderPlacedEvent notification, CancellationToken cancellationToken)
        {
            //insert product also purchased
            InsertProductAlsoPurchased(notification.Order);
            //update customer
            UpdateCustomer(notification.Order);

            //Insert Loyalty Points History
            InsertLoyaltyPointsHistory(notification.Order);

            return Task.CompletedTask;
        }

        protected Task InsertProductAlsoPurchased(Order order)
        {
            foreach (var item in order.OrderItems)
            {
                foreach (var it in order.OrderItems.Where(x => x.ProductId != item.ProductId))
                {
                    var productPurchase = new ProductAlsoPurchased();
                    productPurchase.ProductId = item.ProductId;
                    productPurchase.OrderId = order.Id;
                    productPurchase.CreatedOrderOnUtc = order.CreatedOnUtc;
                    productPurchase.Quantity = it.Quantity;
                    productPurchase.StoreId = order.StoreId;
                    productPurchase.ProductId2 = it.ProductId;
                    _productAlsoPurchasedRepository.InsertAsync(productPurchase);
                }
            }
            return Task.CompletedTask;


        }

        protected Task UpdateCustomer(Order order)
        {
            //Updated field "free shipping" after added a new order
            _customerService.UpdateCustomerField(order.CustomerId, x => x.FreeShipping, false);

            //Update field Last purchase date after added a new order
            _customerService.UpdateCustomerField(order.CustomerId, x => x.LastPurchaseDateUtc, order.CreatedOnUtc);

            //Update field Last update cart date after added a new order
            _customerService.UpdateCustomerField(order.CustomerId, x => x.LastUpdateCartDateUtc, null);

            //Update field has contribution after added a new order
            _customerService.UpdateCustomerField(order.CustomerId, x => x.HasContributions, true);

            return Task.CompletedTask;
        }

        protected Task InsertLoyaltyPointsHistory(Order order)
        {
            //loyalty points history
            if (order.RedeemedLoyaltyPointsAmount > 0)
            {
                _loyaltyPointsService.AddLoyaltyPointsHistory(order.CustomerId,
                    -order.RedeemedLoyaltyPoints, order.StoreId,
                    $"Loyalty points redeemed for order {order.OrderNumber}",
                    order.Id, order.RedeemedLoyaltyPointsAmount);
            }

            return Task.CompletedTask;
        }
    }
}
