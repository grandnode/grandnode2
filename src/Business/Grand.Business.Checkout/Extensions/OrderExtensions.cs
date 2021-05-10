using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using System;
using System.Linq;

namespace Grand.Business.Checkout.Extensions
{
    public static class OrderExtensions
    {
        /// <summary>
        /// Gets a value indicating - download is allowed
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="orderItem">Order item to check</param>
        /// <param name="product">Product</param>
        /// <returns>True if download is allowed; otherwise, false.</returns>
        public static bool IsDownloadAllowed(this Order order, OrderItem orderItem, Product product)
        {
            if (orderItem == null)
                return false;

            if (order == null || order.Deleted)
                return false;

            //order status
            if (order.OrderStatusId == (int)OrderStatusSystem.Cancelled)
                return false;

            if (product == null || !product.IsDownload)
                return false;

            //payment status
            switch (product.DownloadActivationTypeId)
            {
                case DownloadActivationType.WhenOrderIsPaid:
                    {
                        if (order.PaymentStatusId == PaymentStatus.Paid && order.PaidDateUtc.HasValue)
                        {
                            //expiration date
                            if (product.DownloadExpirationDays.HasValue)
                            {
                                if (order.PaidDateUtc.Value.AddDays(product.DownloadExpirationDays.Value) > DateTime.UtcNow)
                                {
                                    return true;
                                }
                            }
                            else
                            {
                                return true;
                            }
                        }
                    }
                    break;
                case DownloadActivationType.Manually:
                    {
                        if (orderItem.IsDownloadActivated)
                        {
                            //expiration date
                            if (product.DownloadExpirationDays.HasValue)
                            {
                                if (order.CreatedOnUtc.AddDays(product.DownloadExpirationDays.Value) > DateTime.UtcNow)
                                {
                                    return true;
                                }
                            }
                            else
                            {
                                return true;
                            }
                        }
                    }
                    break;
                default:
                    break;
            }

            return false;
        }
        /// <summary>
        /// Gets a value indicating whether license download is allowed
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="orderItem">Order item to check</param>
        /// <param name="product">Product</param>
        /// <returns>True if license download is allowed; otherwise, false.</returns>
        public static bool IsLicenseDownloadAllowed(this Order order, OrderItem orderItem, Product product)
        {
            if (orderItem == null)
                return false;

            return !string.IsNullOrEmpty(orderItem.LicenseDownloadId) && IsDownloadAllowed(order, orderItem, product);
        }

        /// <summary>
        /// Gets a value indicating whether an order has items to be added to a shipment
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>A value indicating whether an order has items to be added to a shipment</returns>
        public static bool HasItemsToAddToShipment(this Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            foreach (var orderItem in order.OrderItems)
            {
                //we can ship only shippable products
                if (!orderItem.IsShipEnabled)
                    continue;

                if (orderItem.OpenQty <= 0 || orderItem.Status == OrderItemStatus.Close)
                    continue;

                //yes, we have at least one item to create a new shipment
                return true;
            }
            return false;
        }

        /// <summary>
        /// Indicates whether a order's tag exists
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="orderTagId">Order tag identifier</param>
        /// <returns>Result</returns>
        public static bool OrderTagExists(this Order order, OrderTag orderTag)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            bool result = order.OrderTags.FirstOrDefault(t => t == orderTag.Id) != null;
            return result;
        }
    }
}
