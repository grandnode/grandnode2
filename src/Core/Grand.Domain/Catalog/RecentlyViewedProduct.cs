namespace Grand.Domain.Catalog;

public class RecentlyViewedProduct : BaseEntity
{
    public string CustomerId { get; set; } = null!;
    public string ProductId { get; set; } = null!;
}