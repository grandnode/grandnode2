namespace Grand.Domain.Catalog;

public class ProductDeleted : Product
{
    public DateTime DeletedOnUtc { get; set; }
}