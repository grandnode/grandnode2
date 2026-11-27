using Grand.Domain.Localization;
using Grand.Web.Models.Catalog;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Catalog;

public class GetViewSortSizeOptions : IRequest<(CatalogPagingFilteringModel pagingFilteringModel,
    CatalogPagingFilteringModel command)>
{
    public required Language Language { get; set; }
    public required CatalogPagingFilteringModel PagingFilteringModel { get; set; }
    public required CatalogPagingFilteringModel Command { get; set; }
    public bool AllowCustomersToSelectPageSize { get; set; }
    public string? PageSizeOptions { get; set; }
    public int PageSize { get; set; }
}