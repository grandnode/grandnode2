using Grand.Business.Catalog.Commands.Models;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Infrastructure.Extensions;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Data;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Catalog.Services.Products
{
    /// <summary>
    /// Out of stock subscription service
    /// </summary>
    public partial class OutOfStockSubscriptionService : IOutOfStockSubscriptionService
    {
        #region Fields

        private readonly IRepository<OutOfStockSubscription> _outOfStockSubscriptionRepository;
        private readonly IMediator _mediator;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="outOfStockSubscriptionRepository">Out of stock subscription repository</param>
        /// <param name="messageProviderService">Message provider service</param>
        /// <param name="IMediator">Mediator</param>
        public OutOfStockSubscriptionService(IRepository<OutOfStockSubscription> outOfStockSubscriptionRepository,
            IMediator mediator)
        {
            _outOfStockSubscriptionRepository = outOfStockSubscriptionRepository;
            _mediator = mediator;
        }

        #endregion

        #region Methods

       
        /// <summary>
        /// Gets all subscriptions
        /// </summary>
        /// <param name="customerId">Customer identifier</param>
        /// <param name="storeId">Store identifier; pass "" to load all records</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Subscriptions</returns>
        public virtual async Task<IPagedList<OutOfStockSubscription>> GetAllSubscriptionsByCustomerId(string customerId,
            string storeId = "", int pageIndex = 0, int pageSize = int.MaxValue)
        {
            var query = from p in _outOfStockSubscriptionRepository.Table
                        select p;

            //customer
            query = query.Where(biss => biss.CustomerId == customerId);
            //store
            if (!String.IsNullOrEmpty(storeId))
                query = query.Where(biss => biss.StoreId == storeId);

            query = query.OrderByDescending(biss => biss.CreatedOnUtc);

            return await PagedList<OutOfStockSubscription>.Create(query, pageIndex, pageSize);
        }



        /// <summary>
        /// Gets all subscriptions
        /// </summary>
        /// <param name="customerId">Customer id</param>
        /// <param name="productId">Product identifier</param>
        /// <param name="attributes">Attributes</param>
        /// <param name="storeId">Store identifier</param>
        /// <param name="warehouseId">Warehouse identifier</param>
        /// <returns>Subscriptions</returns>
        public virtual async Task<OutOfStockSubscription> FindSubscription(string customerId, string productId, IList<CustomAttribute> attributes, string storeId, string warehouseId)
        {
            var query = from biss in _outOfStockSubscriptionRepository.Table
                        orderby biss.CreatedOnUtc descending
                        where biss.CustomerId == customerId &&
                              biss.ProductId == productId &&
                              biss.StoreId == storeId &&
                              biss.WarehouseId == warehouseId
                        select biss;

            var outOfStockSubscriptionlist = await Task.FromResult(query.ToList());
            if (attributes != null && attributes.Any())
                outOfStockSubscriptionlist = outOfStockSubscriptionlist.Where(x => x.Attributes.All(y => attributes.Any(z => z.Key == y.Key && z.Value == y.Value))).ToList();

            return outOfStockSubscriptionlist.FirstOrDefault();
        }

        /// <summary>
        /// Gets a subscription
        /// </summary>
        /// <param name="subscriptionId">Subscription identifier</param>
        /// <returns>Subscription</returns>
        public virtual async Task<OutOfStockSubscription> GetSubscriptionById(string subscriptionId)
        {
            var subscription = await _outOfStockSubscriptionRepository.GetByIdAsync(subscriptionId);
            return subscription;
        }

        /// <summary>
        /// Inserts subscription
        /// </summary>
        /// <param name="subscription">Subscription</param>
        public virtual async Task InsertSubscription(OutOfStockSubscription subscription)
        {
            if (subscription == null)
                throw new ArgumentNullException(nameof(subscription));

            await _outOfStockSubscriptionRepository.InsertAsync(subscription);

            //event notification
            await _mediator.EntityInserted(subscription);
        }

        /// <summary>
        /// Updates subscription
        /// </summary>
        /// <param name="subscription">Subscription</param>
        public virtual async Task UpdateSubscription(OutOfStockSubscription subscription)
        {
            if (subscription == null)
                throw new ArgumentNullException(nameof(subscription));

            await _outOfStockSubscriptionRepository.UpdateAsync(subscription);

            //event notification
            await _mediator.EntityUpdated(subscription);
        }
        /// <summary>
        /// Delete a out of stock subscription
        /// </summary>
        /// <param name="subscription">Subscription</param>
        public virtual async Task DeleteSubscription(OutOfStockSubscription subscription)
        {
            if (subscription == null)
                throw new ArgumentNullException(nameof(subscription));

            await _outOfStockSubscriptionRepository.DeleteAsync(subscription);

            //event notification
            await _mediator.EntityDeleted(subscription);
        }

        /// <summary>
        /// Send notification to subscribers
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="warehouse">Warehouse ident</param>
        /// <returns>Number of sent email</returns>
        public virtual async Task SendNotificationsToSubscribers(Product product, string warehouse)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            var subscriptions = await _mediator.Send(new SendNotificationsToSubscribersCommand()
            {
                Product = product,
                Warehouse = warehouse,
            });

            for (var i = 0; i <= subscriptions.Count - 1; i++)
                await DeleteSubscription(subscriptions[i]);
        }

        /// <summary>
        /// Send notification to subscribers
        /// </summary>
        /// <param name="product">Product</param>
        /// <param name="attributes">Attribute</param>
        /// <param name="warehouse">Warehouse ident</param>
        /// <returns>Number of sent email</returns>
        public virtual async Task SendNotificationsToSubscribers(Product product, IList<CustomAttribute> attributes, string warehouse)
        {
            if (product == null)
                throw new ArgumentNullException(nameof(product));

            var subscriptions = await _mediator.Send(new SendNotificationsToSubscribersCommand()
            {
                Product = product,
                Warehouse = warehouse,
                Attributes = attributes
            });

            for (var i = 0; i <= subscriptions.Count - 1; i++)
                await DeleteSubscription(subscriptions[i]);

        }

        #endregion
    }
}
