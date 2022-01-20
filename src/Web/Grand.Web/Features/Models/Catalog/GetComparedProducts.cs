using Grand.Domain.Catalog;
using MediatR;

namespace Grand.Web.Features.Models.Catalog
{
    public class GetComparedProducts : IRequest<IList<Product>>
    {

    }
}
