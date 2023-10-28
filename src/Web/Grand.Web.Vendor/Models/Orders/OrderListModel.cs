using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Grand.Web.Vendor.Models.Orders
{
    public class OrderListModel : BaseModel
    {
        public OrderListModel()
        {
            AvailableOrderStatuses = new List<SelectListItem>();
            AvailablePaymentStatuses = new List<SelectListItem>();
            AvailableShippingStatuses = new List<SelectListItem>();
            AvailableWarehouses = new List<SelectListItem>();
            AvailablePaymentMethods = new List<SelectListItem>();
            AvailableCountries = new List<SelectListItem>();
            AvailableOrderTags = new List<SelectListItem>();
        }

        [GrandResourceDisplayName("Vendor.Orders.List.StartDate")]
        [UIHint("DateNullable")]
        public DateTime? StartDate { get; set; }

        [GrandResourceDisplayName("Vendor.Orders.List.EndDate")]
        [UIHint("DateNullable")]
        public DateTime? EndDate { get; set; }

        public string CustomerId { get; set; }

        [GrandResourceDisplayName("Vendor.Orders.List.BillingEmail")]
        public string BillingEmail { get; set; }

        [GrandResourceDisplayName("Vendor.Orders.List.BillingLastName")]
        public string BillingLastName { get; set; }

        [GrandResourceDisplayName("Vendor.Orders.List.OrderStatus")]
        public int OrderStatusId { get; set; }

        [GrandResourceDisplayName("Vendor.Orders.List.PaymentStatus")]
        public int PaymentStatusId { get; set; }

        [GrandResourceDisplayName("Vendor.Orders.List.ShippingStatus")]
        public int ShippingStatusId { get; set; }

        [GrandResourceDisplayName("Vendor.Orders.List.PaymentMethod")]
        public string PaymentMethodSystemName { get; set; }

        [GrandResourceDisplayName("Vendor.Orders.List.Warehouse")]
        public string WarehouseId { get; set; }

        [GrandResourceDisplayName("Vendor.Orders.List.Product")]
        public string ProductId { get; set; }

        [GrandResourceDisplayName("Vendor.Orders.List.BillingCountry")]
        public string BillingCountryId { get; set; }
        

        [GrandResourceDisplayName("Vendor.Orders.List.OrderGuid")]
        
        public string OrderGuid { get; set; }

        [GrandResourceDisplayName("Vendor.Orders.List.GoDirectlyToNumber")]
        
        public string GoDirectlyToNumber { get; set; }

        [GrandResourceDisplayName("Vendor.Orders.List.OrderTagId")]
        public string OrderTag { get; set; }

        public IList<SelectListItem> AvailableOrderStatuses { get; set; }
        public IList<SelectListItem> AvailablePaymentStatuses { get; set; }
        public IList<SelectListItem> AvailableShippingStatuses { get; set; }
        public IList<SelectListItem> AvailableWarehouses { get; set; }
        public IList<SelectListItem> AvailablePaymentMethods { get; set; }
        public IList<SelectListItem> AvailableCountries { get; set; }
        public IList<SelectListItem> AvailableOrderTags { get; set; } 
    }
}