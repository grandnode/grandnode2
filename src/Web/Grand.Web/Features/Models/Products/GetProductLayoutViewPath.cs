using MediatR;

namespace Grand.Web.Features.Models.Products
{
    public class GetProductLayoutViewPath : IRequest<string>
    {
        public string ProductLayoutId { get; set; }
    }
}
