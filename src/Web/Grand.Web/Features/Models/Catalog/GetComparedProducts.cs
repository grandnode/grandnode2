using Grand.Domain.Catalog;
using MediatR;
using System.Collections.Generic;

namespace Grand.Web.Features.Models.Catalog
{
    public class GetComparedProducts : IRequest<IList<Product>>
    {

    }
}
