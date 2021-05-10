using Grand.Domain.Catalog;
using MediatR;
using System.Collections.Generic;

namespace Grand.Business.Catalog.Queries.Models
{
    public class GetRecommendedProductsQuery : IRequest<IList<Product>>
    {
        public string[] CustomerGroupIds { get; set; }
    }
}
