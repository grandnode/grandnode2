namespace Grand.Domain.Catalog;

public class RecentlyViewedProduct : BaseEntity
{
    public string CustomerId { get; set; }
    public string ProductId { get; set; }
}