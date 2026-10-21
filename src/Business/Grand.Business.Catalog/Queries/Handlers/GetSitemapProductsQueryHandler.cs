using Grand.Business.Core.Queries.Catalog;
using Grand.Data;
using Grand.Data.LiteDb;
using Grand.Domain.Catalog;
using Grand.Infrastructure.Configuration;
using Grand.Mediator;

namespace Grand.Business.Catalog.Queries.Handlers;

public class GetSitemapProductsQueryHandler : IRequestHandler<GetSitemapProductsQuery, IList<SitemapProduct>>
{
    private readonly AccessControlConfig _accessControlConfig;
    private readonly CatalogSettings _catalogSettings;
    private readonly IRepository<Product> _productRepository;

    public GetSitemapProductsQueryHandler(
        IRepository<Product> productRepository,
        CatalogSettings catalogSettings,
        AccessControlConfig accessControlConfig)
    {
        _productRepository = productRepository;
        _catalogSettings = catalogSettings;
        _accessControlConfig = accessControlConfig;
    }

    public async Task<IList<SitemapProduct>> Handle(GetSitemapProductsQuery request,
        CancellationToken cancellationToken)
    {
        //the same products GetSearchProductsQuery returns for a storefront search without keywords
        var query = _productRepository.Table.Where(p => p.Published && p.VisibleIndividually);

        if (!_catalogSettings.IgnoreFilterableAvailableStartEndDateTime)
        {
            var nowUtc = DateTime.UtcNow;
            query = query.Where(p =>
                (p.AvailableStartDateTimeUtc == null || p.AvailableStartDateTimeUtc < nowUtc) &&
                (p.AvailableEndDateTimeUtc == null || p.AvailableEndDateTimeUtc > nowUtc));
        }

        if (!_accessControlConfig.IgnoreAcl)
        {
            var groupIds = request.CustomerGroupIds?.ToArray() ?? [];
            query = query.Where(p => !p.LimitedToGroups || groupIds.Any(x => p.CustomerGroups.Contains(x)));
        }

        if (!string.IsNullOrEmpty(request.StoreId) && !_accessControlConfig.IgnoreStoreLimitations)
            query = query.Where(p => p.Stores.Any(y => y == request.StoreId) || !p.LimitedToStores);

        //keyset paging: a skip would rescan every earlier batch
        if (!string.IsNullOrEmpty(request.AfterId))
            query = query.Where(p => string.Compare(p.Id, request.AfterId) > 0);

        //LiteDB materialises the whole collection on every query, so batching there would read the catalog
        //once per batch: the rest of the catalog comes back in one go instead
        IQueryable<Product> ordered = query.OrderBy(p => p.Id);
        if (_productRepository is not LiteDBRepository<Product>)
            ordered = ordered.Take(request.PageSize);

        //only the fields of a sitemap entry leave the database, not the whole product
        var languageId = request.LanguageId;
        var batch = ordered.Select(p => new {
            p.Id,
            p.SeName,
            TranslatedSeName = p.Locales.Where(l => l.LanguageId == languageId && l.LocaleKey == "SeName")
                .Select(l => l.LocaleValue).FirstOrDefault(),
            PictureId = p.ProductPictures.Select(x => x.PictureId).FirstOrDefault(),
            p.UpdatedOnUtc,
            p.CreatedOnUtc
        });

        return (await _productRepository.ToListAsync(batch, cancellationToken))
            .Select(p => new SitemapProduct(p.Id,
                string.IsNullOrEmpty(p.TranslatedSeName) ? p.SeName : p.TranslatedSeName,
                p.PictureId, p.UpdatedOnUtc ?? p.CreatedOnUtc))
            .ToList();
    }
}
