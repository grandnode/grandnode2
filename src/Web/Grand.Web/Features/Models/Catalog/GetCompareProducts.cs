using Grand.Web.Models.Catalog;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Catalog;

public class GetCompareProducts : IRequest<CompareProductsModel>
{
    public int? PictureProductThumbSize { get; set; }
}