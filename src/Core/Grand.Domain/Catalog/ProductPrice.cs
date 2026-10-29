using Grand.Domain.Audit;

namespace Grand.Domain.Catalog;

public class ProductPrice : SubBaseEntity
{
    [AuditReference(typeof(Product))]
    public string ProductId { get; set; }
    public string CurrencyCode { get; set; }
    public double Price { get; set; }
}