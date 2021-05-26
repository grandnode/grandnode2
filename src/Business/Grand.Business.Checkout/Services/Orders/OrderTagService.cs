using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using Grand.Domain.Data;
using Grand.Domain.Orders;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Services.Orders
{
    public partial class OrderTagService : IOrderTagService
    {

        #region Fields

        private readonly IRepository<OrderTag> _orderTagRepository;
        private readonly IRepository<Order> _orderRepository;
        private readonly ICacheBase _cacheBase;
        private readonly IMediator _mediator;

        #endregion

        #region Ctor

        public OrderTagService(IRepository<OrderTag> orderTagRepository,
            IRepository<Order> orderRepository,
            ICacheBase cacheBase,
            IMediator mediator
            )
        {
            _orderTagRepository = orderTagRepository;
            _orderRepository = orderRepository;
            _mediator = mediator;
            _cacheBase = cacheBase;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Get order's  count for each of existing order tag
        /// </summary>
        /// <param name="storeId">Store identifier</param>
        /// <returns>Dictionary of "order's tag ID : order's count"</returns>
        private async Task<Dictionary<string, int>> GetOrderCount(string orderTagId)
        {
            string key = string.Format(CacheKey.ORDERTAG_COUNT_KEY, orderTagId);
            return await _cacheBase.GetAsync(key, async () =>
            {
                var query = from ot in _orderTagRepository.Table
                            select ot;

                var dictionary = new Dictionary<string, int>();
                foreach (var tag in query.ToList())
                {
                    dictionary.Add(tag.Id, tag.Count);
                }
                return await Task.FromResult(dictionary);
            });
        }

        #endregion

        #region Methods

        
        /// <summary>
        /// Gets all order tags
        /// </summary>
        /// <returns>Order tags</returns>
        public virtual async Task<IList<OrderTag>> GetAllOrderTags()
        {
            return await Task.FromResult(_orderTagRepository.Table.ToList());
        }

        /// <summary>
        /// Gets order's tag by id
        /// </summary>
        /// <param name="orderTagId">Order's tag identifier</param>
        /// <returns>Order's tag</returns>
        public virtual Task<OrderTag> GetOrderTagById(string orderTagId)
        {
            return _orderTagRepository.GetByIdAsync(orderTagId);
        }

        /// <summary>
        /// Gets order's tag by name
        /// </summary>
        /// <param name="name">Order's tag name</param>
        /// <returns>Order's tag</returns>
        public virtual Task<OrderTag> GetOrderTagByName(string name)
        {
            var query = from pt in _orderTagRepository.Table
                        where pt.Name == name
                        select pt;

            return Task.FromResult(query.FirstOrDefault());
        }

        /// <summary>
        /// Inserts a order's tag
        /// </summary>
        /// <param name="orderTag">Order's tag</param>
        public virtual async Task InsertOrderTag(OrderTag orderTag)
        {
            if (orderTag == null)
                throw new ArgumentNullException(nameof(orderTag));

            await _orderTagRepository.InsertAsync(orderTag);

            //cache
            await _cacheBase.RemoveByPrefix(CacheKey.ORDERTAG_PATTERN_KEY);

            //event notification
            await _mediator.EntityInserted(orderTag);
        }

        /// <summary>
        /// Updating a order's tag
        /// </summary>
        /// <param name="orderTag">Order tag</param>
        public virtual async Task UpdateOrderTag(OrderTag orderTag)
        {
            if (orderTag == null)
                throw new ArgumentNullException(nameof(orderTag));

            await _orderTagRepository.UpdateAsync(orderTag);

            //cache
            await _cacheBase.RemoveByPrefix(CacheKey.ORDERTAG_PATTERN_KEY);

            //event notification
            await _mediator.EntityUpdated(orderTag);
        }
        /// <summary>
        /// Delete an order's tag
        /// </summary>
        /// <param name="orderTag">Order's tag</param>
        public virtual async Task DeleteOrderTag(OrderTag orderTag)
        {
            if (orderTag == null)
                throw new ArgumentNullException(nameof(orderTag));

            //update orders
            await _orderRepository.Pull(string.Empty, x => x.OrderTags, orderTag.Id, true);

            //delete tag
            await _orderTagRepository.DeleteAsync(orderTag);

            //cache
            await _cacheBase.RemoveByPrefix(CacheKey.ORDERTAG_PATTERN_KEY);
            await _cacheBase.RemoveByPrefix(CacheKey.ORDERS_PATTERN_KEY);

            //event notification
            await _mediator.EntityDeleted(orderTag);
        }

        /// <summary>
        /// Attach a tag to the order
        /// </summary>
        /// <param name="orderTag">Order's identification</param>
        public virtual async Task AttachOrderTag(string orderTagId, string orderId)
        {
            //assign to product
            await _orderRepository.AddToSet(orderId, x => x.OrderTags, orderTagId);

            var orderTag = await GetOrderTagById(orderTagId);
            //update product tag
            await _orderTagRepository.UpdateField(orderTagId, x => x.Count, orderTag.Count + 1);

            //cache
            await _cacheBase.RemoveAsync(string.Format(CacheKey.ORDERS_BY_ID_KEY, orderId));
            await _cacheBase.RemoveAsync(string.Format(CacheKey.ORDERTAG_COUNT_KEY, orderTagId));

            //event notification
            await _mediator.EntityUpdated(orderTag);
        }

        // <summary>
        /// Detach a tag from the order
        /// </summary>
        /// <param name="orderTag">Order Tag</param>
        public virtual async Task DetachOrderTag(string orderTagId, string orderId)
        {
            await _orderRepository.Pull(orderId, x => x.OrderTags, orderTagId);

            var orderTag = await GetOrderTagById(orderTagId);
            //update product tag
            await _orderTagRepository.UpdateField(orderTagId, x => x.Count, orderTag.Count - 1);

            //cache
            await _cacheBase.RemoveAsync(string.Format(CacheKey.ORDERS_BY_ID_KEY, orderId));
            await _cacheBase.RemoveAsync(string.Format(CacheKey.ORDERTAG_COUNT_KEY, orderTagId));
        }

        /// <summary>
        /// Get number of orders
        /// </summary>
        /// <param name="orderTagId">Order's tag identifier</param>
        /// <param name="storeId">Store identifier</param>
        /// <returns>Number of orders</returns>
        public virtual async Task<int> GetOrderCount(string orderTagId, string storeId)
        {
            var dictionary = await GetOrderCount(orderTagId);
            if (dictionary.ContainsKey(orderTagId))
                return dictionary[orderTagId];

            return 0;
        }

        #endregion
    }
}
