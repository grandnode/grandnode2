using Grand.Domain.Catalog;
using Grand.Domain.Localization;
using Grand.Web.Models.Catalog;
using Grand.Mediator;

namespace Grand.Web.Features.Models.Products;

public class GetProductSpecification : IRequest<IList<ProductSpecificationModel>>
{
    public required Product Product { get; set; }
    public required Language Language { get; set; }
}