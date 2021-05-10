using Grand.Business.Checkout.Extensions;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Checkout.Tests.Extensions
{
    [TestClass]
    public class OrderExtensionsTests
    {
        [TestMethod]
        public void IsDownloadAllowed_ReturnExpectedResult()
        {
            var order1 = new Order() { PaymentStatusId = Domain.Payments.PaymentStatus.Paid, PaidDateUtc = DateTime.UtcNow };
            Order order2 = null;
            var product1 = new Product() { DownloadActivationTypeId = DownloadActivationType.WhenOrderIsPaid, IsDownload = true };
            var product2 = new Product() { DownloadActivationTypeId = DownloadActivationType.Manually, IsDownload = true };
            var product3 = new Product() { IsDownload = false };
            var orderItem = new OrderItem() { IsDownloadActivated = true };

            Assert.IsTrue(order1.IsDownloadAllowed(orderItem, product1));
            Assert.IsTrue(order1.IsDownloadAllowed(orderItem, product2));
            Assert.IsFalse(order1.IsDownloadAllowed(null, product1));
            Assert.IsFalse(order1.IsDownloadAllowed(orderItem, null));
            Assert.IsFalse(order1.IsDownloadAllowed(null, null));
            Assert.IsFalse(order1.IsDownloadAllowed(orderItem, product3));
            Assert.IsFalse(order2.IsDownloadAllowed(orderItem, product2));
        }

        [TestMethod]
        public void IsLicenseDownloadAllowed_ReturnExpectedResults()
        {
            var order1 = new Order() { PaymentStatusId = Domain.Payments.PaymentStatus.Paid, PaidDateUtc = DateTime.UtcNow };
            Order order2 = null;
            var product1 = new Product() { DownloadActivationTypeId = DownloadActivationType.WhenOrderIsPaid, IsDownload = true };
            var product2 = new Product() { DownloadActivationTypeId = DownloadActivationType.Manually, IsDownload = true };
            var product3 = new Product() { IsDownload = false };
            var orderItem = new OrderItem() { IsDownloadActivated = true, LicenseDownloadId = "idlicense" };
            var orderItem2 = new OrderItem() { IsDownloadActivated = true, LicenseDownloadId = null };
            Assert.IsTrue(order1.IsLicenseDownloadAllowed(orderItem, product1));
            Assert.IsTrue(order1.IsLicenseDownloadAllowed(orderItem, product2));
            Assert.IsFalse(order1.IsLicenseDownloadAllowed(orderItem2, product1));
            Assert.IsFalse(order1.IsLicenseDownloadAllowed(orderItem2, product2));
            Assert.IsFalse(order2.IsLicenseDownloadAllowed(orderItem, product2));
        }

        [TestMethod]
        public void HasItemsToAddToShipment_ReturnExpectedResult()
        {
            var order = new Order();
            order.OrderItems.Add(new OrderItem() { IsShipEnabled = false, OpenQty = 1 });
            Assert.IsFalse(order.HasItemsToAddToShipment());
            order.OrderItems.Add(new OrderItem() { IsShipEnabled = true, OpenQty = 1 });
            Assert.IsTrue(order.HasItemsToAddToShipment());
        }

        [TestMethod]
        public void HasItemsToAddToShipment_NullOrder_ThrowException()
        {
            Order order = null;
            Assert.ThrowsException<ArgumentNullException>(()=>order.HasItemsToAddToShipment());
        }

        [TestMethod]
        public void OrderTagExists_ReturnExpectedResult()
        {
            var order = new Order();
            var tag = new OrderTag() { Id = "id" };
            order.OrderTags.Add("1");
            Assert.IsFalse(order.OrderTagExists(tag));
            order.OrderTags.Add("id");
            Assert.IsTrue(order.OrderTagExists(tag));
        }

        [TestMethod]
        public void OrderTagExists_NullOrder_ThrowException()
        {
            Order order = null;
            Assert.ThrowsException<ArgumentNullException>(() => order.HasItemsToAddToShipment());
        }
    }
}
