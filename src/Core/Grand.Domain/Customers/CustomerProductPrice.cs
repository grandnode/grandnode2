using Grand.Domain.Audit;

namespace Grand.Domain.Customers;

public class CustomerProductPrice : BaseEntity
{
    [AuditReference(typeof(Customer))]
    public string CustomerId { get; set; } = null!;
    public string ProductId { get; set; } = null!;
    public double Price { get; set; }
}