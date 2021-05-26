using Grand.Business.Catalog.Commands.Models;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Messages.Interfaces;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.SharedKernel.Extensions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.System.Commands.Handlers.Catalog
{
    public class SendNotificationsToSubscribersCommandHandler : IRequestHandler<SendNotificationsToSubscribersCommand, IList<OutOfStockSubscription>>
    {
        private readonly ICustomerService _customerService;
        private readonly IMessageProviderService _messageProviderService;
        private readonly IRepository<OutOfStockSubscription> _outOfStockSubscriptionRepository;

        public SendNotificationsToSubscribersCommandHandler(
            ICustomerService customerService,
            IMessageProviderService messageProviderService,
            IRepository<OutOfStockSubscription> outOfStockSubscriptionRepository)
        {
            _customerService = customerService;
            _messageProviderService = messageProviderService;
            _outOfStockSubscriptionRepository = outOfStockSubscriptionRepository;
        }

        public async Task<IList<OutOfStockSubscription>> Handle(SendNotificationsToSubscribersCommand request, CancellationToken cancellationToken)
        {
            if (request.Product == null)
                throw new ArgumentNullException(nameof(request.Product));

            int result = 0;

            var subscriptions = await GetAllSubscriptionsByProductId(request.Product.Id, request.Attributes, request.Warehouse);
            foreach (var subscription in subscriptions)
            {
                var customer = await _customerService.GetCustomerById(subscription.CustomerId);
                //ensure that customer is registered (simple and fast way)
                if (customer != null && CommonHelper.IsValidEmail(customer.Email))
                {
                    var customerLanguageId = customer.GetUserFieldFromEntity<string>(SystemCustomerFieldNames.LanguageId, subscription.StoreId);
                    await _messageProviderService.SendBackinStockMessage(customer, request.Product, subscription, customerLanguageId);
                    result++;
                }
            }

            return subscriptions;

        }

        /// <summary>
        /// Gets all subscriptions
        /// </summary>
        /// <param name="productId">Product identifier</param>
        /// <param name="attributes">Attributes</param>
        /// <param name="warehouseId">Store identifier; pass "" to load all records</param>
        /// <returns>Subscriptions</returns>
        private async Task<IList<OutOfStockSubscription>> GetAllSubscriptionsByProductId(string productId, IList<CustomAttribute> attributes, string warehouseId)
        {
            var query = _outOfStockSubscriptionRepository.Table
                .Where(biss => biss.ProductId == productId)
                .OrderByDescending(biss => biss.CreatedOnUtc).ToList();

            //warehouse
            if (!string.IsNullOrEmpty(warehouseId))
                query = query.Where(biss => biss.WarehouseId == warehouseId).ToList();

            //attributes
            if (attributes != null && attributes.Any())
                foreach (var item in attributes)
                {
                    query = query.Where(x => x.Attributes.Any(y => y.Key == item.Key && y.Value == item.Value)).ToList();
                }

            return await Task.FromResult(query.ToList());
        }
    }
}
