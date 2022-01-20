using Grand.Domain.Customers;
using Grand.Domain.Stores;
using MediatR;

namespace Grand.Web.Features.Models.Catalog
{
    public class GetChildCategoryIds : IRequest<IList<string>>
    {
        public string ParentCategoryId { get; set; }
        public Customer Customer { get; set; }
        public Store Store { get; set; }

    }
}
