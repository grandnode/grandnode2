using Grand.Domain.Customers;
using Grand.Domain.Localization;
using Grand.Domain.Stores;
using Grand.Web.Models.Catalog;
using MediatR;

namespace Grand.Web.Features.Models.Catalog;

public class GetCategoryAll : IRequest<CategoryListModel>
{
    public Store Store { get; set; }
    public Customer Customer { get; set; }
    public Language Language { get; set; }
    public CategoryPagingModel Command { get; set; }
}