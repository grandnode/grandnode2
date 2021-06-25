using Grand.Business.Catalog.Queries.Models;
using Grand.Infrastructure;
using Grand.Domain.Catalog;
using Grand.Domain.Customers;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.Products;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Components
{
    public class CustomerRecommendedProductsViewComponent : BaseViewComponent
    {

        #region Fields

        private readonly IWorkContext _workContext;
        private readonly IMediator _mediator;
        private readonly CatalogSettings _catalogSettings;

        #endregion

        #region Constructors

        public CustomerRecommendedProductsViewComponent(
            IWorkContext workContext,
            IMediator mediator,
            CatalogSettings catalogSettings)
        {
            _workContext = workContext;
            _mediator = mediator;
            _catalogSettings = catalogSettings;
        }

        #endregion

        #region Invoker

        public async Task<IViewComponentResult> InvokeAsync(int? productThumbPictureSize)
        {
            if (!_catalogSettings.RecommendedProductsEnabled)
                return Content("");

            var products = await _mediator.Send(new GetRecommendedProductsQuery() { CustomerGroupIds = _workContext.CurrentCustomer.GetCustomerGroupIds() });

            if (!products.Any())
                return Content("");

            var model = await _mediator.Send(new GetProductOverview()
            {
                PreparePictureModel = true,
                PreparePriceModel = true,
                ProductThumbPictureSize = productThumbPictureSize,
                Products = products,
            });

            return View(model);
        }

        #endregion

    }
}
