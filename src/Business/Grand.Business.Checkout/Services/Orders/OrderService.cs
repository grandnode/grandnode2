using Grand.Business.Checkout.Commands.Models.Orders;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Checkout.Queries.Models.Orders;
using Grand.Domain;
using Grand.Domain.Data;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Shipping;
using Grand.Infrastructure.Extensions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Services.Orders
{
    /// <summary>
    /// Order service
    /// </summary>
    public partial class OrderService : IOrderService
    {
        #region Fields

        private readonly IRepository<Order> _orderRepository;
        private readonly IRepository<OrderNote> _orderNoteRepository;
        private readonly IMediator _mediator;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="orderRepository">Order repository</param>
        /// <param name="orderNoteRepository">Order note repository</param>
        /// <param name="productAlsoPurchasedRepository">Product also purchased repository</param>
        /// <param name="mediator">Mediator</param>
        public OrderService(IRepository<Order> orderRepository,
            IRepository<OrderNote> orderNoteRepository,
            IMediator mediator
            )
        {
            _orderRepository = orderRepository;
            _orderNoteRepository = orderNoteRepository;
            _mediator = mediator;
        }

        #endregion

        #region Methods

        #region Orders

        /// <summary>
        /// Gets an order
        /// </summary>
        /// <param name="orderId">The order identifier</param>
        /// <returns>Order</returns>
        public virtual Task<Order> GetOrderById(string orderId)
        {
            return _orderRepository.GetByIdAsync(orderId);
        }


        /// <summary>
        /// Gets an order
        /// </summary>
        /// <param name="orderId">The order item identifier</param>
        /// <returns>Order</returns>
        public virtual Task<Order> GetOrderByOrderItemId(string orderItemId)
        {
            var query = from o in _orderRepository.Table
                        where o.OrderItems.Any(x => x.Id == orderItemId)
                        select o;

            return Task.FromResult(query.FirstOrDefault());
        }
        /// <summary>
        /// Gets an order
        /// </summary>
        /// <param name="orderNumber">The order number</param>
        /// <returns>Order</returns>
        public virtual Task<Order> GetOrderByNumber(int orderNumber)
        {
            return Task.FromResult(_orderRepository.Table.Where(x => x.OrderNumber == orderNumber).FirstOrDefault());
        }

        /// <summary>
        /// Gets orders by code
        /// </summary>
        /// <param name="code">The order code</param>
        /// <returns>Order</returns>
        public virtual async Task<IList<Order>> GetOrdersByCode(string code)
        {
            if (string.IsNullOrEmpty(code))
                return new List<Order>();

            return await Task.FromResult(_orderRepository.Table.Where(x => x.Code == code.ToUpperInvariant()).ToList());
        }


        /// <summary>
        /// Get orders by identifiers
        /// </summary>
        /// <param name="orderIds">Order identifiers</param>
        /// <returns>Order</returns>
        public virtual async Task<IList<Order>> GetOrdersByIds(string[] orderIds)
        {
            if (orderIds == null || orderIds.Length == 0)
                return new List<Order>();

            var query = from o in _orderRepository.Table
                        where orderIds.Contains(o.Id)
                        select o;
            var orders = await Task.FromResult(query.ToList());
            //sort by passed identifiers
            var sortedOrders = new List<Order>();
            foreach (string id in orderIds)
            {
                var order = orders.Find(x => x.Id == id);
                if (order != null)
                    sortedOrders.Add(order);
            }
            return sortedOrders;
        }

        /// <summary>
        /// Gets an order
        /// </summary>
        /// <param name="orderGuid">The order identifier</param>
        /// <returns>Order</returns>
        public virtual Task<Order> GetOrderByGuid(Guid orderGuid)
        {
            var query = from o in _orderRepository.Table
                        where o.OrderGuid == orderGuid
                        select o;
            return Task.FromResult(query.FirstOrDefault());
        }

        /// <summary>
        /// Search orders
        /// </summary>
        /// <param name="storeId">Store identifier; 0 to load all orders</param>
        /// <param name="vendorId">Vendor identifier; null to load all orders</param>
        /// <param name="customerId">Customer identifier; 0 to load all orders</param>
        /// <param name="productId">Product identifier which was purchased in an order; 0 to load all orders</param>
        /// <param name="affiliateId">Affiliate identifier; 0 to load all orders</param>
        /// <param name="warehouseId">Warehouse identifier, only orders with products from a specified warehouse will be loaded; 0 to load all orders</param>
        /// <param name="billingCountryId">Billing country identifier; 0 to load all orders</param>
        /// <param name="ownerId">Owner identifier</param>
        /// <param name="salesemployeeId">Sales employee identifier</param>
        /// <param name="paymentMethodSystemName">Payment method system name; null to load all records</param>
        /// <param name="createdFromUtc">Created date from (UTC); null to load all records</param>
        /// <param name="createdToUtc">Created date to (UTC); null to load all records</param>
        /// <param name="os">Order status; null to load all orders</param>
        /// <param name="ps">Order payment status; null to load all orders</param>
        /// <param name="ss">Order shipment status; null to load all orders</param>
        /// <param name="billingEmail">Billing email. Leave empty to load all records.</param>
        /// <param name="orderNotes">Search in order notes. Leave empty to load all records.</param>
        /// <param name="orderGuid">Search by order GUID (Global unique identifier) or part of GUID. Leave empty to load all orders.</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="orderTagId">Order tag identifier</param>
        /// <returns>Orders</returns>
        public virtual async Task<IPagedList<Order>> SearchOrders(string storeId = "",
            string vendorId = "", string customerId = "",
            string productId = "", string affiliateId = "", string warehouseId = "",
            string billingCountryId = "", string ownerId = "", string salesemployeeId = "", string paymentMethodSystemName = null,
            DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
            int? os = null, PaymentStatus? ps = null, ShippingStatus? ss = null,
            string billingEmail = null, string billingLastName = "", string orderGuid = null,
            string orderCode = null, int pageIndex = 0, int pageSize = int.MaxValue, string orderTagId = "")
        {
            var querymodel = new GetOrderQuery()
            {
                AffiliateId = affiliateId,
                BillingCountryId = billingCountryId,
                BillingEmail = billingEmail,
                BillingLastName = billingLastName,
                CreatedFromUtc = createdFromUtc,
                CreatedToUtc = createdToUtc,
                CustomerId = customerId,
                OrderGuid = orderGuid,
                OrderCode = orderCode,
                Os = os,
                PageIndex = pageIndex,
                PageSize = pageSize,
                PaymentMethodSystemName = paymentMethodSystemName,
                ProductId = productId,
                Ps = ps,
                Ss = ss,
                StoreId = storeId,
                VendorId = vendorId,
                WarehouseId = warehouseId,
                OrderTagId = orderTagId,
                OwnerId = ownerId,
                SalesEmployeeId = salesemployeeId
            };
            var query = await _mediator.Send(querymodel);
            return await PagedList<Order>.Create(query, pageIndex, pageSize);
        }

        /// <summary>
        /// Inserts an order
        /// </summary>
        /// <param name="order">Order</param>
        public virtual async Task InsertOrder(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            var orderExists = _orderRepository.Table.OrderByDescending(x => x.OrderNumber).Select(x => x.OrderNumber).FirstOrDefault();
            order.OrderNumber = orderExists != 0 ? orderExists + 1 : 1;

            await _orderRepository.InsertAsync(order);

            //event notification
            await _mediator.EntityInserted(order);
        }

        /// <summary>
        /// Updates the order
        /// </summary>
        /// <param name="order">The order</param>
        public virtual async Task UpdateOrder(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            await _orderRepository.UpdateAsync(order);

            //event notification
            await _mediator.EntityUpdated(order);
        }

        /// <summary>
        /// Cancel UnPaid Orders and has pending status
        /// </summary>
        /// <param name="expirationDateUTC">Date at which all unPaid orders and has pending status Would be Canceled</param>
        public async Task CancelExpiredOrders(DateTime expirationDateUTC)
        {
            var orders = _orderRepository.Table
              .Where(o => o.CreatedOnUtc < expirationDateUTC && o.PaymentStatusId == PaymentStatus.Pending &&
              o.OrderStatusId == (int)OrderStatusSystem.Pending && (o.ShippingStatusId == ShippingStatus.Pending 
              || o.ShippingStatusId == ShippingStatus.ShippingNotRequired))
              .ToList();

            foreach (var order in orders)
                await _mediator.Send(new CancelOrderCommand() { Order = order, NotifyCustomer = true });
        }

        #endregion

        #region Orders items

        /// <summary>
        /// Gets an item
        /// </summary>
        /// <param name="orderItemGuid">Order identifier</param>
        /// <returns>Order item</returns>
        public virtual Task<OrderItem> GetOrderItemByGuid(Guid orderItemGuid)
        {
            var query = from order in _orderRepository.Table
                        from orderItem in order.OrderItems
                        select orderItem;

            query = from orderItem in query
                    where orderItem.OrderItemGuid == orderItemGuid
                    select orderItem;

            return Task.FromResult(query.FirstOrDefault());
        }

        /// <summary>
        /// Delete an order item
        /// </summary>
        /// <param name="orderItem">The order item</param>
        public virtual async Task DeleteOrderItem(OrderItem orderItem)
        {
            if (orderItem == null)
                throw new ArgumentNullException(nameof(orderItem));

            var order = await GetOrderByOrderItemId(orderItem.Id);

            await _orderRepository.PullFilter(order.Id, x => x.OrderItems, x => x.Id == orderItem.Id);

            //event notification
            await _mediator.EntityDeleted(orderItem);
        }

        #endregion

        #region Orders notes

        /// <summary>
        /// Deletes an order note
        /// </summary>
        /// <param name="orderNote">The order note</param>
        public virtual async Task DeleteOrderNote(OrderNote orderNote)
        {
            if (orderNote == null)
                throw new ArgumentNullException(nameof(orderNote));

            await _orderNoteRepository.DeleteAsync(orderNote);

            //event notification
            await _mediator.EntityDeleted(orderNote);
        }

        /// <summary>
        /// Deletes an order note
        /// </summary>
        /// <param name="orderNote">The order note</param>
        public virtual async Task InsertOrderNote(OrderNote orderNote)
        {
            if (orderNote == null)
                throw new ArgumentNullException(nameof(orderNote));

            await _orderNoteRepository.InsertAsync(orderNote);

            //event notification
            await _mediator.EntityInserted(orderNote);
        }

        public virtual async Task<IList<OrderNote>> GetOrderNotes(string orderId)
        {
            var query = from orderNote in _orderNoteRepository.Table
                        where orderNote.OrderId == orderId
                        orderby orderNote.CreatedOnUtc descending
                        select orderNote;

            return await Task.FromResult(query.ToList());
        }

        /// <summary>
        /// Get ordernote by id
        /// </summary>
        /// <param name="ordernoteId">Order note identifier</param>
        /// <returns>OrderNote</returns>
        public virtual Task<OrderNote> GetOrderNote(string ordernoteId)
        {
            return Task.FromResult(_orderNoteRepository.Table.Where(x => x.Id == ordernoteId).FirstOrDefault());
        }


        #endregion

        #endregion
    }
}
