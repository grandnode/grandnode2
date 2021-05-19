using Grand.Business.Catalog.Events.Models;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using MediatR;
using MongoDB.Driver;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Events.Handlers
{
    public class UpdateProductOnCartEventHandler : INotificationHandler<UpdateProductOnCartEvent>
    {
        private readonly IRepository<Customer> _customerRepository;

        public UpdateProductOnCartEventHandler(IRepository<Customer> customerRepository)
        {
            _customerRepository = customerRepository;
        }

        public async Task Handle(UpdateProductOnCartEvent notification, CancellationToken cancellationToken)
        {
            var builderCustomer = Builders<Customer>.Filter;
            var filterCustomer = builderCustomer.ElemMatch(x => x.ShoppingCartItems, y => y.ProductId == notification.Product.Id);
            await _customerRepository.Collection.Find(filterCustomer).ForEachAsync(async (cs) =>
            {
                foreach (var item in cs.ShoppingCartItems.Where(x => x.ProductId == notification.Product.Id))
                {
                    item.AdditionalShippingChargeProduct = notification.Product.AdditionalShippingCharge;
                    item.IsFreeShipping = notification.Product.IsFreeShipping;
                    item.IsGiftVoucher = notification.Product.IsGiftVoucher;
                    item.IsShipEnabled = notification.Product.IsShipEnabled;
                    item.IsTaxExempt = notification.Product.IsTaxExempt;

                    await _customerRepository.UpdateToSet(cs.Id, x => x.ShoppingCartItems, z => z.Id, item.Id, item);
                }
            }
            );
        }
    }
}
