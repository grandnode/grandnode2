using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Queries.Catalog;
using Grand.Data;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Infrastructure.Caching;
using Grand.Infrastructure.Caching.Constants;
using Grand.Infrastructure.Configuration;
using Grand.Mediator;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Grand.Business.Catalog.Queries.Handlers;

public class GetSearchProductsQueryHandler : IRequestHandler<GetSearchProductsQuery, (IPagedList<Product> products, IList<string> filterableSpecificationAttributeOptionIds)>
{
    //the missing index is reported once per process, not on every search; a race may report it twice
    private static bool _missingTextIndexReported;

    //how long a listing total and its filter options may lag behind writes that do not clear them
    private const int ListingSummaryCacheMinutes = 5;

    //the version only changes when the listing pattern is cleared; its expiry merely starts a new one
    private const int ListingVersionCacheMinutes = 60;

    private readonly AccessControlConfig _accessControlConfig;
    private readonly ICacheBase _cacheBase;
    private readonly CatalogSettings _catalogSettings;
    private readonly IDatabaseContext _databaseContext;
    private readonly DatabaseConfig _databaseConfig;
    private readonly ILogger<GetSearchProductsQueryHandler> _logger;
    private readonly IRepository<Product> _productRepository;
    private readonly ISpecificationAttributeService _specificationAttributeService;

    public GetSearchProductsQueryHandler(
        IRepository<Product> productRepository,
        ISpecificationAttributeService specificationAttributeService,
        ICacheBase cacheBase,
        CatalogSettings catalogSettings,
        AccessControlConfig accessControlConfig,
        DatabaseConfig databaseConfig,
        IDatabaseContext databaseContext,
        ILogger<GetSearchProductsQueryHandler> logger)
    {
        _productRepository = productRepository;
        _specificationAttributeService = specificationAttributeService;
        _cacheBase = cacheBase;
        _catalogSettings = catalogSettings;
        _accessControlConfig = accessControlConfig;
        _databaseConfig = databaseConfig;
        _databaseContext = databaseContext;
        _logger = logger;
    }
    public async Task<(IPagedList<Product> products, IList<string> filterableSpecificationAttributeOptionIds)> Handle(GetSearchProductsQuery request, CancellationToken cancellationToken)
    {
        // Clean up request parameters
        CleanupRequestParameters(request);

        // Access control list. Allowed customer groups; the customer is read only when the filter applies
        var allowedCustomerGroupsIds = request.CustomerGroupIds?.ToArray()
                                       ?? (request.ShowHidden || _accessControlConfig.IgnoreAcl
                                           ? []
                                           : request.Customer.GetCustomerGroupIds());

        var search = TextSearchTerms(request);
        if (search != null)
            try
            {
                return await Search(request, allowedCustomerGroupsIds, search, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                //no text index - not created, dropped or being built: scan as before
                if (!_missingTextIndexReported)
                {
                    _missingTextIndexReported = true;
                    _logger.LogWarning(ex,
                        "Product keyword search through the text index {IndexName} failed and scans instead. Create the index - see UseProductTextSearch in appsettings.json",
                        ProductTextIndex.Name);
                }
            }

        return await Search(request, allowedCustomerGroupsIds, null, cancellationToken);
    }

    private async Task<(IPagedList<Product> products, IList<string> filterableSpecificationAttributeOptionIds)> Search(
        GetSearchProductsQuery request, string[] allowedCustomerGroupsIds, string search,
        CancellationToken cancellationToken)
    {
        // Build base query; a text search narrows the candidates to products with a whole query word,
        // ApplyKeywordFiltering still decides the match
        var query = search == null
            ? _productRepository.Table
            : _databaseContext.TextSearch(_productRepository, search);
        query = FilterQueryable(request, query, allowedCustomerGroupsIds);

        // Keep a copy for specifications filtering
        var querySpecification = query;

        // Apply specification filters
        (query, querySpecification, var specificationOptionIds) =
            await ApplySpecificationFilters(request, query, querySpecification);

        // A listing page reads its total and filter options from a short-lived cache: both depend on the filters
        // only, not on the page or the order, and each costs a pass over every match
        if (CanCacheListingSummary(request))
            return await SearchWithCachedSummary(request, allowedCustomerGroupsIds, specificationOptionIds, query,
                querySpecification, cancellationToken);

        // Order the results
        query = OrderByQueryable(request, query);

        // The page and the filterable specification option ids are independent, so both queries run at once
        return await WhenBoth(
            token => _productRepository.PagedAsync(query, request.PageIndex, request.PageSize, token),
            token => ShouldLoadFilterableSpecifications(request)
                ? GetFilterableSpecificationAttributeOptionIds(querySpecification, token)
                : Task.FromResult<IList<string>>(new List<string>()),
            cancellationToken);
    }

    private async Task<(IPagedList<Product> products, IList<string> filterableSpecificationAttributeOptionIds)>
        SearchWithCachedSummary(GetSearchProductsQuery request, string[] allowedCustomerGroupsIds,
            IList<string> specificationOptionIds, IQueryable<Product> query, IQueryable<Product> querySpecification,
            CancellationToken cancellationToken)
    {
        var version = await _cacheBase.GetAsync(CacheKey.PRODUCTS_LISTING_VERSION_KEY,
            () => Task.FromResult(Guid.NewGuid().ToString("N")), ListingVersionCacheMinutes);
        var key = string.Format(CacheKey.PRODUCTS_LISTING_SUMMARY_KEY, version,
            ListingSummaryHash(request, allowedCustomerGroupsIds, specificationOptionIds));

        //same normalization as IRepository.PagedAsync
        var pageSize = request.PageSize <= 0 ? 1 : request.PageSize;
        var skip = (int)Math.Clamp((long)request.PageIndex * pageSize, 0, int.MaxValue);

        var (items, summary) = await WhenBoth(
            token => _productRepository.ToListAsync(OrderByQueryable(request, query).Skip(skip).Take(pageSize), token),
            //a miss is computed once for every request waiting on it, so it does not run under the token of
            //whichever request came first - one aborted request would discard it for all; each one still stops
            //waiting when it is cancelled itself
            token => _cacheBase.GetAsync(key,
                    () => LoadListingSummary(request, query, querySpecification, CancellationToken.None),
                    ListingSummaryCacheMinutes)
                .WaitAsync(token),
            cancellationToken);

        //a short page is the last one and gives the exact total. A full page proves its products exist, so a cached
        //total lagging behind writes never puts the page shown past the last one
        var totalCount = items.Count < pageSize && (items.Count > 0 || skip == 0)
            ? skip + items.Count
            : items.Count == pageSize
                ? Math.Max(summary.TotalCount, skip + items.Count)
                : summary.TotalCount;

        return (new PagedList<Product>(items, request.PageIndex, pageSize, totalCount),
            summary.FilterableSpecificationAttributeOptionIds.ToList());
    }

    /// <summary>
    ///     Only listing pages opt in, and only with filters the catalog itself bounds: free text and arbitrary numbers
    ///     from the query string would each add a cache entry
    /// </summary>
    private static bool CanCacheListingSummary(GetSearchProductsQuery request)
    {
        return request.CacheListingSummary && !request.ShowHidden &&
               string.IsNullOrWhiteSpace(request.Keywords) &&
               (request.SpecificationOptions == null || !request.SpecificationOptions.Any()) &&
               request.PriceMin == null && request.PriceMax == null &&
               (request.Rating == null || request.Rating is >= 0 and <= 5 && request.Rating % 1 == 0);
    }

    private async Task<ListingSummary> LoadListingSummary(GetSearchProductsQuery request, IQueryable<Product> query,
        IQueryable<Product> querySpecification, CancellationToken cancellationToken)
    {
        var countTask = _productRepository.CountAsync(query, cancellationToken);
        var filterableTask = ShouldLoadFilterableSpecifications(request)
            ? GetFilterableSpecificationAttributeOptionIds(querySpecification, cancellationToken)
            : Task.FromResult<IList<string>>(new List<string>());

        await Task.WhenAll(countTask, filterableTask);

        return new ListingSummary(await countTask, (await filterableTask).ToArray());
    }

    /// <summary>
    ///     Hash of every request value that changes which products match; page and order are left out on purpose.
    ///     Specifications count by the options that resolved, so ids that match no option do not add entries
    /// </summary>
    private string ListingSummaryHash(GetSearchProductsQuery request, string[] allowedCustomerGroupsIds,
        IList<string> specificationOptionIds)
    {
        var filters = new StringBuilder();

        //each value is prefixed with its length, so no id or tag can run into the next field. Invariant culture:
        //the request culture follows the customer's language and would split one listing in two
        void Add(object value)
        {
            var text = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
            filters.Append(text.Length).Append(':').Append(text);
        }

        void AddSet(IEnumerable<string> values)
        {
            var sorted = values?.Distinct().Order(StringComparer.Ordinal).ToList() ?? [];
            Add(sorted.Count);
            foreach (var value in sorted) Add(value);
        }

        AddSet(request.CategoryIds);
        Add(request.FeaturedProducts);
        Add(request.BrandId);
        Add(request.CollectionId);
        Add(request.StoreId);
        Add(request.VendorId);
        Add(request.WarehouseId);
        Add(request.ProductType);
        Add(request.VisibleIndividuallyOnly);
        Add(request.MarkedAsNewOnly);
        Add(request.ShowOnHomePage);
        Add(request.OverridePublished);
        Add(request.Rating);
        Add(request.ProductTag);
        AddSet(allowedCustomerGroupsIds);
        AddSet(specificationOptionIds);
        Add(ShouldLoadFilterableSpecifications(request));

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(filters.ToString())));
    }

    private sealed record ListingSummary(int TotalCount, string[] FilterableSpecificationAttributeOptionIds);

    /// <summary>
    ///     Runs two independent queries at once; the first failure cancels the other one instead of waiting for it
    /// </summary>
    private static async Task<(T1, T2)> WhenBoth<T1, T2>(Func<CancellationToken, Task<T1>> first,
        Func<CancellationToken, Task<T2>> second, CancellationToken cancellationToken)
    {
        using var queriesCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // The first one starts first - LiteDB completes synchronously, so there it still runs before the second
        var firstTask = CancelOnFailure(first(queriesCancellation.Token), queriesCancellation);
        var secondTask = CancelOnFailure(second(queriesCancellation.Token), queriesCancellation);

        try
        {
            await Task.WhenAll(firstTask, secondTask);
        }
        catch when (cancellationToken.IsCancellationRequested)
        {
            //an aborted request may surface as a driver fault of the other query; report it as cancelled
            //so Handle does not mistake it for a missing text index
            cancellationToken.ThrowIfCancellationRequested();
            throw;
        }

        return (await firstTask, await secondTask);
    }

    private static async Task<T> CancelOnFailure<T>(Task<T> task, CancellationTokenSource cancellation)
    {
        try
        {
            return await task;
        }
        catch
        {
            await cancellation.CancelAsync();
            throw;
        }
    }

    private static void CleanupRequestParameters(GetSearchProductsQuery request)
    {
        if (request.CategoryIds != null && request.CategoryIds.Contains(""))
            request.CategoryIds.Remove("");
    }

    /// <returns>The filtered queries and the filtered specification option ids that resolved to an attribute</returns>
    private async Task<(IQueryable<Product> query, IQueryable<Product> querySpecification, IList<string> specificationOptionIds)>
        ApplySpecificationFilters(
            GetSearchProductsQuery request,
            IQueryable<Product> query,
            IQueryable<Product> querySpecification)
    {
        IList<string> specificationOptionIds = [];

        // Apply filtered specs
        if (request.FilteredSpecs != null && request.FilteredSpecs.Any())
        {
            (query, querySpecification, specificationOptionIds) =
                await ApplyFilteredSpecifications(request, query, querySpecification);
        }

        // Apply specification options
        if (request.SpecificationOptions != null && request.SpecificationOptions.Any())
        {
            query = ApplySpecificationOptions(request, query);
        }

        return (query, querySpecification, specificationOptionIds);
    }

    private async Task<(IQueryable<Product> query, IQueryable<Product> querySpecification, IList<string> specificationOptionIds)>
        ApplyFilteredSpecifications(
            GetSearchProductsQuery request,
            IQueryable<Product> query,
            IQueryable<Product> querySpecification)
    {
        var specAttributeMapping = await BuildSpecificationAttributeMapping(request.FilteredSpecs, querySpecification);

        // Apply filters for each specification attribute
        foreach (var item in specAttributeMapping.Dictionary)
        {
            query = query.Where(x => x.ProductSpecificationAttributes.Any(y =>
                y.SpecificationAttributeId == item.Key &&
                y.AllowFiltering &&
                item.Value.Contains(y.SpecificationAttributeOptionId)));
        }

        return (query, specAttributeMapping.QuerySpecification,
            specAttributeMapping.Dictionary.Values.SelectMany(x => x).ToList());
    }

    private async Task<(Dictionary<string, List<string>> Dictionary, IQueryable<Product> QuerySpecification)>
        BuildSpecificationAttributeMapping(IList<string> filteredSpecs, IQueryable<Product> querySpecification)
    {
        var dictionary = new Dictionary<string, List<string>>();

        foreach (var key in filteredSpecs)
        {
            var specification = await _specificationAttributeService.GetSpecificationAttributeByOptionId(key);
            if (specification == null) continue;

            if (!dictionary.ContainsKey(specification.Id))
            {
                // Add to dictionary
                dictionary.Add(specification.Id, new List<string>());

                // Update querySpecification
                querySpecification = querySpecification.Where(x =>
                    x.ProductSpecificationAttributes.Any(y =>
                        y.SpecificationAttributeId == specification.Id && y.AllowFiltering));
            }

            dictionary[specification.Id].Add(key);
        }

        return (dictionary, querySpecification);
    }

    private static IQueryable<Product> ApplySpecificationOptions(GetSearchProductsQuery request, IQueryable<Product> query)
    {
        return query.Where(x => x.ProductSpecificationAttributes.Any(y =>
            request.SpecificationOptions.Contains(y.SpecificationAttributeOptionId)));
    }

    private bool ShouldLoadFilterableSpecifications(GetSearchProductsQuery request)
    {
        return request.LoadFilterableSpecificationAttributeOptionIds &&
               !_catalogSettings.IgnoreFilterableSpecAttributeOption;
    }

    private async Task<IList<string>> GetFilterableSpecificationAttributeOptionIds(
        IQueryable<Product> querySpecification, CancellationToken cancellationToken)
    {
        var filterSpecExists = querySpecification.Where(x =>
            x.ProductSpecificationAttributes.Any(x => x.AllowFiltering));

        var spec = from p in filterSpecExists
                   from item in p.ProductSpecificationAttributes
                   select item;

        //only the option ids leave the database, not the grouped attributes
        var optionIds = spec.Where(x => x.AllowFiltering)
            .Select(x => x.SpecificationAttributeOptionId)
            .Distinct();

        return await _productRepository.ToListAsync(optionIds, cancellationToken);
    }

    private IQueryable<Product> FilterQueryable(GetSearchProductsQuery request, IQueryable<Product> query, string[] allowedCustomerGroupsIds)
    {
        query = ApplyCategoryFiltering(request, query);
        query = ApplyBrandFiltering(request, query);
        query = ApplyCollectionFiltering(request, query);
        query = ApplyPublishedFiltering(request, query);
        query = ApplyVisibilityFiltering(request, query);
        query = ApplyProductTypeFiltering(request, query);
        query = ApplyHomePageFiltering(request, query);
        query = ApplyPriceFiltering(request, query);
        query = ApplyRatingFiltering(request, query);
        query = ApplyAvailabilityFiltering(request, query);
        query = ApplyNewProductFiltering(request, query);
        query = ApplyKeywordFiltering(request, query);
        query = ApplyAccessControlFiltering(request, query, allowedCustomerGroupsIds);
        query = ApplyStoreFiltering(request, query);
        query = ApplyVendorFiltering(request, query);
        query = ApplyWarehouseFiltering(request, query);
        query = ApplyTagFiltering(request, query);

        return query;
    }

    private static IQueryable<Product> ApplyCategoryFiltering(GetSearchProductsQuery request, IQueryable<Product> query)
    {
        if (request.CategoryIds != null && request.CategoryIds.Any())
        {
            if (request.FeaturedProducts.HasValue)
                query = query.Where(x => x.ProductCategories.Any(y => request.CategoryIds.Contains(y.CategoryId) && y.IsFeaturedProduct == request.FeaturedProducts));
            else
                query = query.Where(x => x.ProductCategories.Any(y => request.CategoryIds.Contains(y.CategoryId)));
        }
        return query;
    }

    private static IQueryable<Product> ApplyBrandFiltering(GetSearchProductsQuery request, IQueryable<Product> query)
    {
        if (!string.IsNullOrEmpty(request.BrandId))
            query = query.Where(x => x.BrandId == request.BrandId);
        return query;
    }

    private static IQueryable<Product> ApplyCollectionFiltering(GetSearchProductsQuery request, IQueryable<Product> query)
    {
        if (!string.IsNullOrEmpty(request.CollectionId))
        {
            if (request.FeaturedProducts.HasValue)
                query = query.Where(x => x.ProductCollections.Any(y => y.CollectionId == request.CollectionId && y.IsFeaturedProduct == request.FeaturedProducts));
            else
                query = query.Where(x => x.ProductCollections.Any(y => y.CollectionId == request.CollectionId));
        }
        return query;
    }

    private static IQueryable<Product> ApplyPublishedFiltering(GetSearchProductsQuery request, IQueryable<Product> query)
    {
        if (!request.OverridePublished.HasValue)
        {
            if (!request.ShowHidden)
                query = query.Where(p => p.Published);
        }
        else if (request.OverridePublished.Value)
        {
            query = query.Where(p => p.Published);
        }
        else if (!request.OverridePublished.Value)
        {
            query = query.Where(p => !p.Published);
        }
        return query;
    }

    private static IQueryable<Product> ApplyVisibilityFiltering(GetSearchProductsQuery request, IQueryable<Product> query)
    {
        if (request.VisibleIndividuallyOnly)
            query = query.Where(p => p.VisibleIndividually);
        return query;
    }

    private static IQueryable<Product> ApplyProductTypeFiltering(GetSearchProductsQuery request, IQueryable<Product> query)
    {
        if (request.ProductType.HasValue)
            query = query.Where(p => p.ProductTypeId == request.ProductType);
        return query;
    }

    private static IQueryable<Product> ApplyHomePageFiltering(GetSearchProductsQuery request, IQueryable<Product> query)
    {
        if (request.ShowOnHomePage.HasValue)
            query = query.Where(p => p.ShowOnHomePage == request.ShowOnHomePage.Value);
        return query;
    }

    private static IQueryable<Product> ApplyPriceFiltering(GetSearchProductsQuery request, IQueryable<Product> query)
    {
        if (request.PriceMin.HasValue)
            query = query.Where(p => p.Price >= request.PriceMin.Value);
        if (request.PriceMax.HasValue)
            query = query.Where(p => p.Price <= request.PriceMax.Value);
        return query;
    }

    private static IQueryable<Product> ApplyRatingFiltering(GetSearchProductsQuery request, IQueryable<Product> query)
    {
        if (request.Rating.HasValue)
            query = query.Where(p => p.AvgRating >= request.Rating);
        return query;
    }

    private IQueryable<Product> ApplyAvailabilityFiltering(GetSearchProductsQuery request, IQueryable<Product> query)
    {
        var nowUtc = DateTime.UtcNow;
        if (!request.ShowHidden && !_catalogSettings.IgnoreFilterableAvailableStartEndDateTime)
            query = query.Where(p => (p.AvailableStartDateTimeUtc == null || p.AvailableStartDateTimeUtc < nowUtc) && (p.AvailableEndDateTimeUtc == null || p.AvailableEndDateTimeUtc > nowUtc));
        return query;
    }

    private static IQueryable<Product> ApplyNewProductFiltering(GetSearchProductsQuery request, IQueryable<Product> query)
    {
        if (request.MarkedAsNewOnly)
        {
            var nowUtc = DateTime.UtcNow;
            query = query.Where(p => p.MarkAsNew);
            query = query.Where(p => (!p.MarkAsNewStartDateTimeUtc.HasValue || p.MarkAsNewStartDateTimeUtc.Value < nowUtc) && (!p.MarkAsNewEndDateTimeUtc.HasValue || p.MarkAsNewEndDateTimeUtc.Value > nowUtc));
        }
        return query;
    }

    private string TextSearchTerms(GetSearchProductsQuery request)
    {
        //staff search by fragments and partial SKUs. The text index does not cover descriptions
        if (!_databaseConfig.UseProductTextSearch || request.ShowHidden || request.SearchDescriptions ||
            string.IsNullOrWhiteSpace(request.Keywords))
            return null;

        //quotes and minus would be read as a phrase and a negation
        var search = request.Keywords.Replace('"', ' ').Replace('-', ' ');

        //$text finds no word in punctuation alone, nor a part of a text written without spaces between words
        return search.Any(char.IsLetterOrDigit) && !search.Any(IsUnspacedScript) ? search : null;
    }

    private static bool IsUnspacedScript(char c)
    {
        return c is >= '\u0E00' and <= '\u0EFF' //Thai, Lao
            or >= '\u1000' and <= '\u109F' //Myanmar
            or >= '\u1780' and <= '\u17FF' //Khmer
            or >= '\u2E80' and <= '\u9FFF' //CJK punctuation, kana, ideographs
            or >= '\uF900' and <= '\uFAFF' //CJK compatibility ideographs
            or >= '\uFF66' and <= '\uFF9F'; //halfwidth katakana
    }

    private static IQueryable<Product> ApplyKeywordFiltering(GetSearchProductsQuery request, IQueryable<Product> query)
    {
        if (!string.IsNullOrWhiteSpace(request.Keywords))
        {
            if (!request.SearchDescriptions)
                query = query.Where(p => p.Name.ToLower().Contains(request.Keywords.ToLower()) || p.Locales.Any(x => x.LocaleKey == "Name" && x.LocaleValue != null && x.LocaleValue.ToLower().Contains(request.Keywords.ToLower())) || (request.SearchSku && p.Sku != null && p.Sku.ToLower().Contains(request.Keywords.ToLower())));
            else
                query = query.Where(p => (p.Name != null && p.Name.ToLower().Contains(request.Keywords.ToLower())) || (p.ShortDescription != null && p.ShortDescription.ToLower().Contains(request.Keywords.ToLower())) || (p.FullDescription != null && p.FullDescription.ToLower().Contains(request.Keywords.ToLower())) || p.Locales.Any(x => x.LocaleValue != null && x.LocaleValue.ToLower().Contains(request.Keywords.ToLower())) || (request.SearchSku && p.Sku != null && p.Sku.ToLower().Contains(request.Keywords.ToLower())));
        }
        return query;
    }

    private IQueryable<Product> ApplyAccessControlFiltering(GetSearchProductsQuery request, IQueryable<Product> query, string[] allowedCustomerGroupsIds)
    {
        if (!request.ShowHidden && !_accessControlConfig.IgnoreAcl)
            query = from p in query where !p.LimitedToGroups || allowedCustomerGroupsIds.Any(x => p.CustomerGroups.Contains(x)) select p;
        return query;
    }

    private IQueryable<Product> ApplyStoreFiltering(GetSearchProductsQuery request, IQueryable<Product> query)
    {
        if (!string.IsNullOrEmpty(request.StoreId) && !_accessControlConfig.IgnoreStoreLimitations)
            query = query.Where(x => x.Stores.Any(y => y == request.StoreId) || !x.LimitedToStores);
        return query;
    }

    private static IQueryable<Product> ApplyVendorFiltering(GetSearchProductsQuery request, IQueryable<Product> query)
    {
        if (!string.IsNullOrEmpty(request.VendorId))
            query = query.Where(x => x.VendorId == request.VendorId);
        return query;
    }

    private static IQueryable<Product> ApplyWarehouseFiltering(GetSearchProductsQuery request, IQueryable<Product> query)
    {
        if (!string.IsNullOrEmpty(request.WarehouseId))
            query = query.Where(x => (x.UseMultipleWarehouses && x.ProductWarehouseInventory.Any(y => y.WarehouseId == request.WarehouseId)) || (!x.UseMultipleWarehouses && x.WarehouseId == request.WarehouseId));
        return query;
    }

    private static IQueryable<Product> ApplyTagFiltering(GetSearchProductsQuery request, IQueryable<Product> query)
    {
        if (!string.IsNullOrEmpty(request.ProductTag))
            query = query.Where(x => x.ProductTags.Any(y => y == request.ProductTag));
        return query;
    }

    private IQueryable<Product> OrderByQueryable(GetSearchProductsQuery request, IQueryable<Product> query)
    {
        if (request.OrderBy == ProductSortingEnum.Position)
        {
            query = OrderByPosition(request, query);
        }
        else if (request.OrderBy == ProductSortingEnum.NameAsc)
        {
            query = OrderByNameAsc(query);
        }
        else if (request.OrderBy == ProductSortingEnum.NameDesc)
        {
            query = OrderByNameDesc(query);
        }
        else if (request.OrderBy == ProductSortingEnum.PriceAsc)
        {
            query = OrderByPriceAsc(query);
        }
        else if (request.OrderBy == ProductSortingEnum.PriceDesc)
        {
            query = OrderByPriceDesc(query);
        }
        else if (request.OrderBy == ProductSortingEnum.CreatedOn)
        {
            query = OrderByCreatedOn(query);
        }
        else if (request.OrderBy == ProductSortingEnum.OnSale)
        {
            query = OrderByOnSale(query);
        }
        else if (request.OrderBy == ProductSortingEnum.MostViewed)
        {
            query = OrderByMostViewed(query);
        }
        else if (request.OrderBy == ProductSortingEnum.BestSellers)
        {
            query = OrderByBestSellers(query);
        }
        else if (request.OrderBy == ProductSortingEnum.Rating)
        {
            query = OrderByRating(query);
        }

        return query;
    }

    private IQueryable<Product> OrderByPosition(GetSearchProductsQuery request, IQueryable<Product> query)
    {
        if (request.CategoryIds != null && request.CategoryIds.Any())
        {
            query = _catalogSettings.SortingByAvailability
                ? query.OrderBy(x => x.LowStock).ThenBy(x => x.DisplayOrderCategory)
                : query.OrderBy(x => x.DisplayOrderCategory);
        }
        else if (!string.IsNullOrEmpty(request.BrandId))
        {
            query = _catalogSettings.SortingByAvailability
                ? query.OrderBy(x => x.LowStock).ThenBy(x => x.DisplayOrderBrand)
                : query.OrderBy(x => x.DisplayOrderBrand);
        }
        else if (!string.IsNullOrEmpty(request.CollectionId))
        {
            query = _catalogSettings.SortingByAvailability
                ? query.OrderBy(x => x.LowStock).ThenBy(x => x.DisplayOrderCollection)
                : query.OrderBy(x => x.DisplayOrderCollection);
        }
        else
        {
            query = _catalogSettings.SortingByAvailability
                ? query.OrderBy(x => x.LowStock).ThenBy(x => x.Name)
                : query.OrderBy(x => x.Name);
        }

        return query;
    }

    private IQueryable<Product> OrderByNameAsc(IQueryable<Product> query)
    {
        return _catalogSettings.SortingByAvailability
            ? query.OrderBy(x => x.LowStock).ThenBy(x => x.Name)
            : query.OrderBy(x => x.Name);
    }

    private IQueryable<Product> OrderByNameDesc(IQueryable<Product> query)
    {
        return _catalogSettings.SortingByAvailability
            ? query.OrderBy(x => x.LowStock).ThenByDescending(x => x.Name)
            : query.OrderByDescending(x => x.Name);
    }

    private IQueryable<Product> OrderByPriceAsc(IQueryable<Product> query)
    {
        return _catalogSettings.SortingByAvailability
            ? query.OrderBy(x => x.LowStock).ThenBy(x => x.Price)
            : query.OrderBy(x => x.Price);
    }

    private IQueryable<Product> OrderByPriceDesc(IQueryable<Product> query)
    {
        return _catalogSettings.SortingByAvailability
            ? query.OrderBy(x => x.LowStock).ThenByDescending(x => x.Price)
            : query.OrderByDescending(x => x.Price);
    }

    private IQueryable<Product> OrderByCreatedOn(IQueryable<Product> query)
    {
        return _catalogSettings.SortingByAvailability
            ? query.OrderBy(x => x.LowStock).ThenBy(x => x.CreatedOnUtc)
            : query.OrderBy(x => x.CreatedOnUtc);
    }

    private IQueryable<Product> OrderByOnSale(IQueryable<Product> query)
    {
        query = query.OrderBy(x => x.OnSale);

        return _catalogSettings.SortingByAvailability
            ? query.OrderBy(x => x.LowStock).ThenBy(x => x.OnSale)
            : query.OrderBy(x => x.OnSale);
    }

    private IQueryable<Product> OrderByMostViewed(IQueryable<Product> query)
    {
        return _catalogSettings.SortingByAvailability
            ? query.OrderBy(x => x.LowStock).ThenByDescending(x => x.Viewed)
            : query.OrderByDescending(x => x.Viewed);
    }

    private IQueryable<Product> OrderByBestSellers(IQueryable<Product> query)
    {
        query = query.OrderByDescending(x => x.Sold);

        return _catalogSettings.SortingByAvailability
            ? query.OrderBy(x => x.LowStock).ThenByDescending(x => x.Sold)
            : query.OrderByDescending(x => x.Sold);
    }

    private IQueryable<Product> OrderByRating(IQueryable<Product> query)
    {
        return _catalogSettings.SortingByAvailability
            ? query.OrderBy(x => x.LowStock).ThenByDescending(x => x.AvgRating)
            : query.OrderByDescending(x => x.AvgRating).ThenByDescending(x => x.ApprovedTotalReviews);
    
    }
}