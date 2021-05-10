using System.Collections.Generic;

namespace DiscountRules.Standard.Models
{
    public class RequirementProducts
    {
        public RequirementProducts()
        {
            Products = new List<string>();
        }
        public IList<string> Products { get; set; }
    }
}
