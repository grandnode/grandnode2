using Grand.Domain.Audit;

namespace Grand.Domain.Customers;

public class CustomerProductPrice : BaseEntity
{
    [AuditReference(typeof(Customer))]
    public string CustomerId { get; set; }
    public string ProductId { get; set; }
    public double Price { get; set; }
}