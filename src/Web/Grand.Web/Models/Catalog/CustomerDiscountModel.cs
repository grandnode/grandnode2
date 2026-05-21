using Grand.Infrastructure.Models;

namespace Grand.Web.Models.Catalog;

public class CustomerDiscountModel : BaseModel
{
    public string Name { get; set; }
    public bool UsePercentage { get; set; }
    public double DiscountPercentage { get; set; }
    public double DiscountAmount { get; set; }
    public string CurrencyCode { get; set; }
    public DateTime? EndDateUtc { get; set; }
    public bool RequiresCouponCode { get; set; }
}
