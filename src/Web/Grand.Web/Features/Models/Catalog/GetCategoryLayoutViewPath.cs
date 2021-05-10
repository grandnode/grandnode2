using MediatR;

namespace Grand.Web.Features.Models.Catalog
{
    public class GetCategoryLayoutViewPath : IRequest<string>
    {
        public string LayoutId { get; set; }
    }
}
