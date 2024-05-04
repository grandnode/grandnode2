using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using MediatR;

namespace Grand.Business.Core.Queries.Catalog;

public class GetSearchProductsQuery : IRequest<(IPagedList<Product> products, IList<string>
    filterableSpecificationAttributeOptionIds)>
{
    public Customer Customer { get; set; }

    public bool LoadFilterableSpecificationAttributeOptionIds { get; set; } = false;
    public int PageIndex { get; set; } = 0;
    public int PageSize { get; set; } = int.MaxValue;
    public IList<string> CategoryIds { get; set; }
    public string BrandId { get; set; } = "";
    public string CollectionId { get; set; } = "";
    public string StoreId { get; set; } = "";
    public string VendorId { get; set; } = "";
    public string WarehouseId { get; set; } = "";
    public ProductType? ProductType { get; set; }
    public bool VisibleIndividuallyOnly { get; set; }
    public bool MarkedAsNewOnly { get; set; }
    public bool? ShowOnHomePage { get; set; }
    public bool? FeaturedProducts { get; set; }
    public double? PriceMin { get; set; }
    public double? PriceMax { get; set; }
    public double? Rating { get; set; }
    public string ProductTag { get; set; } = "";
    public string Keywords { get; set; }
    public bool SearchDescriptions { get; set; }
    public bool SearchSku { get; set; } = true;
    public bool SearchProductTags { get; set; }
    public string LanguageId { get; set; } = "";
    public IList<string> FilteredSpecs { get; set; }
    public IList<string> SpecificationOptions { get; set; }
    public ProductSortingEnum OrderBy { get; set; } = ProductSortingEnum.Position;
    public bool ShowHidden { get; set; }
    public bool? OverridePublished { get; set; }
}