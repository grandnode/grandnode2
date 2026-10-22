using Grand.Mediator;

namespace Grand.Business.Core.Queries.Catalog;

/// <summary>
///     One batch of the products a sitemap lists: published, visible individually, available now, in the store and
///     visible to the given customer groups. Batches follow product id, so the next one starts after the last id
/// </summary>
public class GetSitemapProductsQuery : IRequest<IList<SitemapProduct>>
{
    public string StoreId { get; set; }
    public IList<string> CustomerGroupIds { get; set; } = [];
    public string LanguageId { get; set; }

    /// <summary>
    ///     Id of the last product of the previous batch; null for the first batch
    /// </summary>
    public string AfterId { get; set; }

    public int PageSize { get; set; } = 1000;
}

/// <summary>
///     The fields of a product a sitemap entry needs
/// </summary>
/// <param name="SeName">Search engine name in the requested language, or the default one</param>
/// <param name="PictureId">The first product picture; null when the product has none</param>
public record SitemapProduct(string Id, string SeName, string PictureId, DateTime UpdatedOnUtc);
