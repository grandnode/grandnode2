using Grand.Api.Models;
using System;

namespace Grand.Api.DTOs.Catalog
{
    public partial class ProductTierPriceDto : BaseApiEntityModel
    {
        public string StoreId { get; set; }
        public string CustomerGroupId { get; set; }
        public int Quantity { get; set; }
        public double Price { get; set; }
        public DateTime? StartDateTimeUtc { get; set; }
        public DateTime? EndDateTimeUtc { get; set; }
    }
}
