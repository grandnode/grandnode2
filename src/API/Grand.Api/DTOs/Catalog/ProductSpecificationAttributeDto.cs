using Grand.Api.Models;
using Grand.Domain.Catalog;

namespace Grand.Api.DTOs.Catalog
{
    public partial class ProductSpecificationAttributeDto : BaseApiEntityModel
    {
        public string SpecificationAttributeId { get; set; }
        public string SpecificationAttributeOptionId { get; set; }
        public string CustomValue { get; set; }
        public bool AllowFiltering { get; set; }
        public bool ShowOnProductPage { get; set; }
        public int DisplayOrder { get; set; }
        public SpecificationAttributeType AttributeTypeId { get; set; }
    }
}
