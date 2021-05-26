using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Domain.Data;
using Grand.Domain.Orders;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Services.Orders
{
    public class OrderStatusService : IOrderStatusService
    {
        private readonly IRepository<OrderStatus> _orderStatusRepository;
        private readonly ICacheBase _cacheBase;
        private readonly IMediator _mediator;

        public OrderStatusService(
            IRepository<OrderStatus> orderStatusRepository,
            ICacheBase cacheBase,
            IMediator mediator)
        {
            _orderStatusRepository = orderStatusRepository;
            _cacheBase = cacheBase;
            _mediator = mediator;
        }

        public virtual async Task<IList<OrderStatus>> GetAll()
        {
            string key = string.Format(CacheKey.ORDER_STATUS_ALL);
            var orderstatuses = await _cacheBase.GetAsync(CacheKey.ORDER_STATUS_ALL, async () =>
            {
                var query = from p in _orderStatusRepository.Table
                            select p;

                query = query.OrderBy(l => l.DisplayOrder);
                return await Task.FromResult(query.ToList());
            });

            return orderstatuses;
        }
        public virtual async Task<OrderStatus> GetById(string id)
        {
            return (await GetAll()).FirstOrDefault(x => x.Id == id);
        }
        public virtual async Task<OrderStatus> GetByStatusId(int statusId)
        {
            return (await GetAll()).FirstOrDefault(x => x.StatusId == statusId);
        }

        public virtual async Task Insert(OrderStatus orderStatus)
        {
            if (orderStatus == null)
                throw new ArgumentNullException(nameof(orderStatus));

            //insert order status
            await _orderStatusRepository.InsertAsync(orderStatus);

            //cache
            await _cacheBase.RemoveByPrefix(CacheKey.ORDER_STATUS_ALL);

            //event notification
            await _mediator.EntityInserted(orderStatus);
        }

        public virtual async Task Update(OrderStatus orderStatus)
        {
            if (orderStatus == null)
                throw new ArgumentNullException(nameof(orderStatus));

            //update order status
            await _orderStatusRepository.UpdateAsync(orderStatus);

            //cache
            await _cacheBase.RemoveByPrefix(CacheKey.ORDER_STATUS_ALL);

            //event notification
            await _mediator.EntityUpdated(orderStatus);
        }
        public virtual async Task Delete(OrderStatus orderStatus)
        {
            if (orderStatus == null)
                throw new ArgumentNullException(nameof(orderStatus));

            if (orderStatus.IsSystem)
                throw new Exception("You can't delete system status");

            await _orderStatusRepository.DeleteAsync(orderStatus);

            //cache
            await _cacheBase.RemoveByPrefix(CacheKey.ORDER_STATUS_ALL);

            //event notification
            await _mediator.EntityDeleted(orderStatus);
        }

    }
}
