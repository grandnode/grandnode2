using Grand.Infrastructure.ModelBinding;
using Grand.Infrastructure.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace Shipping.ShippingPoint.Models
{
    public class ShippingPointModel : BaseEntityModel
    {
        public ShippingPointModel()
        {
            AvailableCountries = new List<SelectListItem>();
            AvailableStores = new List<SelectListItem>();
        }

        [GrandResourceDisplayName("Shipping.ShippingPoint.Fields.ShippingPointName")]
        public string ShippingPointName { get; set; }

        [GrandResourceDisplayName("Shipping.ShippingPoint.Fields.Description")]
        public string Description { get; set; }

        [GrandResourceDisplayName("Shipping.ShippingPoint.Fields.OpeningHours")]
        public string OpeningHours { get; set; }

        [GrandResourceDisplayName("Shipping.ShippingPoint.Fields.PickupFee")]
        public double PickupFee { get; set; }

        public List<SelectListItem> AvailableStores { get; set; }
        [GrandResourceDisplayName("Shipping.ShippingPoint.Fields.Store")]
        public string StoreId { get; set; }

        public string StoreName { get; set; }

        [GrandResourceDisplayName("Shipping.ShippingPoint.Fields.City")]
        public string City { get; set; }

        [GrandResourceDisplayName("Shipping.ShippingPoint.Fields.Address1")]
        public string Address1 { get; set; }

        [GrandResourceDisplayName("Shipping.ShippingPoint.Fields.ZipPostalCode")]
        public string ZipPostalCode { get; set; }

        [GrandResourceDisplayName("Shipping.ShippingPoint.Fields.Country")]
        public string CountryId { get; set; }

        public IList<SelectListItem> AvailableCountries { get; set; }
    }


}