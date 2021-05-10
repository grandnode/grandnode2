using MediatR;

namespace Grand.Web.Features.Models.Catalog
{
    public class GetBrandLayoutViewPath : IRequest<string>
    {
        public string LayoutId { get; set; }
    }
}
