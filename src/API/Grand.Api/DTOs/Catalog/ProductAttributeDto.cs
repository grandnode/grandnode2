using Grand.Api.Models;
using System.Collections.Generic;

namespace Grand.Api.DTOs.Catalog
{
    public partial class ProductAttributeDto : BaseApiEntityModel
    {
        public ProductAttributeDto()
        {
            PredefinedProductAttributeValues = new List<PredefinedProductAttributeValueDto>();
        }
        public string Name { get; set; }
        public string Description { get; set; }
        public IList<PredefinedProductAttributeValueDto> PredefinedProductAttributeValues { get; set; }
    }

    public partial class PredefinedProductAttributeValueDto : BaseApiEntityModel
    {
        public string Name { get; set; }
        public double PriceAdjustment { get; set; }
        public double WeightAdjustment { get; set; }
        public double Cost { get; set; }
        public bool IsPreSelected { get; set; }
        public int DisplayOrder { get; set; }
    }
}
