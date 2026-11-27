using Grand.Domain.Audit;

namespace Grand.Domain.Customers;

public class CustomerProduct : BaseEntity
{
    [AuditReference(typeof(Customer))]
    public string CustomerId { get; set; } = null!;
    public string ProductId { get; set; } = null!;
    public int DisplayOrder { get; set; }
}