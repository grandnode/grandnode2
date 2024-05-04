using Grand.Domain.Catalog;
using Grand.Web.Models.Catalog;
using MediatR;

namespace Grand.Web.Features.Models.Products;

public class GetProductOverview : IRequest<IEnumerable<ProductOverviewModel>>
{
    public IEnumerable<Product> Products { get; set; }
    public bool PreparePriceModel { get; set; } = true;
    public bool PreparePictureModel { get; set; } = true;
    public int? ProductThumbPictureSize { get; set; }
    public bool PrepareSpecificationAttributes { get; set; }
    public bool ForceRedirectionAfterAddingToCart { get; set; }
}