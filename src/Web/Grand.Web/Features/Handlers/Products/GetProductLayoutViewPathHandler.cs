using Grand.Business.Catalog.Interfaces.Products;
using Grand.Web.Features.Models.Products;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Web.Features.Handlers.Products
{
    public class GetProductLayoutViewPathHandler : IRequestHandler<GetProductLayoutViewPath, string>
    {
        private readonly IProductLayoutService _productLayoutService;

        public GetProductLayoutViewPathHandler(IProductLayoutService productLayoutService)
        {
            _productLayoutService = productLayoutService;
        }

        public async Task<string> Handle(GetProductLayoutViewPath request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.ProductLayoutId))
                throw new ArgumentNullException("ProductLayoutId");

            var layout = await _productLayoutService.GetProductLayoutById(request.ProductLayoutId);
            if (layout == null)
                layout = (await _productLayoutService.GetAllProductLayouts()).FirstOrDefault();
            if (layout == null)
                throw new Exception("No default layout could be loaded");
            return layout.ViewPath;

        }
    }
}
