using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Data;
using Grand.Domain.Orders;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Extensions;
using MediatR;

namespace Grand.Business.Checkout.Services.Orders;

public class OrderStatusService : IOrderStatusService
{
    private readonly ICacheBase _cacheBase;
    private readonly IMediator _mediator;
    private readonly IRepository<OrderStatus> _orderStatusRepository;

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
        var orderStatuses = await _cacheBase.GetAsync(CacheKey.ORDER_STATUS_ALL, async () =>
        {
            var query = from p in _orderStatusRepository.Table
                select p;

            query = query.OrderBy(l => l.DisplayOrder);
            return await Task.FromResult(query.ToList());
        });

        return orderStatuses;
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
        ArgumentNullException.ThrowIfNull(orderStatus);

        //insert order status
        await _orderStatusRepository.InsertAsync(orderStatus);

        //cache
        await _cacheBase.RemoveByPrefix(CacheKey.ORDER_STATUS_ALL);

        //event notification
        await _mediator.EntityInserted(orderStatus);
    }

    public virtual async Task Update(OrderStatus orderStatus)
    {
        ArgumentNullException.ThrowIfNull(orderStatus);

        //update order status
        await _orderStatusRepository.UpdateAsync(orderStatus);

        //cache
        await _cacheBase.RemoveByPrefix(CacheKey.ORDER_STATUS_ALL);

        //event notification
        await _mediator.EntityUpdated(orderStatus);
    }

    public virtual async Task Delete(OrderStatus orderStatus)
    {
        ArgumentNullException.ThrowIfNull(orderStatus);

        if (orderStatus.IsSystem)
            throw new Exception("You can't delete system status");

        await _orderStatusRepository.DeleteAsync(orderStatus);

        //cache
        await _cacheBase.RemoveByPrefix(CacheKey.ORDER_STATUS_ALL);

        //event notification
        await _mediator.EntityDeleted(orderStatus);
    }
}