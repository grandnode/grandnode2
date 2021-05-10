using Grand.Api.Models;

namespace Grand.Api.DTOs.Customers
{
    public partial class CustomerGroupDto : BaseApiEntityModel
    {
        public string Name { get; set; }
        public bool FreeShipping { get; set; }
        public bool TaxExempt { get; set; }
        public bool Active { get; set; }
        public bool IsSystem { get; set; }
        public string SystemName { get; set; }
        public bool EnablePasswordLifetime { get; set; }
    }
}
