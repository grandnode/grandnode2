using MediatR;

namespace Grand.Web.Features.Models.Catalog
{
    public class GetCollectionLayoutViewPath : IRequest<string>
    {
        public string LayoutId { get; set; }
    }
}
