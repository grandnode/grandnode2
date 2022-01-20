﻿using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Grand.Web.Admin.Models.Common;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Models.Shipping
{
    public partial class PickupPointModel : BaseEntityModel
    {
        public PickupPointModel()
        {
            this.Address = new AddressModel();
            this.AvailableWarehouses = new List<SelectListItem>();
            this.AvailableStores = new List<SelectListItem>();
        }

        [GrandResourceDisplayName("Admin.Configuration.Shipping.PickupPoint.Fields.Name")]

        public string Name { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Shipping.PickupPoint.Fields.Description")]

        public string Description { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Shipping.PickupPoint.Fields.AdminComment")]

        public string AdminComment { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Shipping.PickupPoint.Fields.Address")]
        public AddressModel Address { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Shipping.PickupPoint.Fields.DisplayOrder")]
        public int DisplayOrder { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Shipping.PickupPoint.Fields.Warehouses")]
        public IList<SelectListItem> AvailableWarehouses { get; set; }

        public string WarehouseId { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Shipping.PickupPoint.Fields.Stores")]
        public IList<SelectListItem> AvailableStores { get; set; }
        public string StoreId { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Shipping.PickupPoint.Fields.PickupFee")]
        public double PickupFee { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Shipping.PickupPoint.Fields.Latitude")]
        public double? Latitude { get; set; }

        [GrandResourceDisplayName("Admin.Configuration.Shipping.PickupPoint.Fields.Longitude")]
        public double? Longitude { get; set; }
    }
}