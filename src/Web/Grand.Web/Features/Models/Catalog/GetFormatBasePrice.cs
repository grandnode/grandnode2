using Grand.Domain.Catalog;
using Grand.Domain.Directory;
using MediatR;
namespace Grand.Web.Features.Models.Catalog
{
    public class GetFormatBasePrice : IRequest<string>
    {
        public Currency Currency { get; set; }
        public Product Product { get; set; }
        public double? ProductPrice { get; set; }
    }
}
