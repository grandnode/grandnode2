using Grand.Domain.Audit;

namespace Grand.Domain.Customers;

public class CustomerProduct : BaseEntity
{
    [AuditReference(typeof(Customer))]
    public string CustomerId { get; set; }
    public string ProductId { get; set; }
    public int DisplayOrder { get; set; }
}