using Grand.Domain.Customers;
using Grand.Web.Models.Catalog;
using MediatR;

namespace Grand.Web.Features.Models.Catalog
{
    public class GetCompareProducts : IRequest<CompareProductsModel>
    {
        public int? PictureProductThumbSize { get; set; }
    }
}
