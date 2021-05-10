using Grand.Business.Catalog.Queries.Handlers;
using Grand.Infrastructure;
using Grand.Domain.Catalog;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.Products;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Components
{
    public class HomePageNewProductsViewComponent : BaseViewComponent
    {
        #region Fields

        private readonly IWorkContext _workContext;
        private readonly IMediator _mediator;
        private readonly CatalogSettings _catalogSettings;

        #endregion

        #region Constructors

        public HomePageNewProductsViewComponent(
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
            if (!_catalogSettings.NewProductsOnHomePage)
                return Content("");

            var products = (await _mediator.Send(new GetSearchProductsQuery()
            {
                Customer = _workContext.CurrentCustomer,
                StoreId = _workContext.CurrentStore.Id,
                VisibleIndividuallyOnly = true,
                MarkedAsNewOnly = true,
                OrderBy = ProductSortingEnum.CreatedOn,
                PageSize = _catalogSettings.NewProductsNumberOnHomePage
            })).products;

            if (!products.Any())
                return Content("");

            var model = await _mediator.Send(new GetProductOverview()
            {
                PreparePictureModel = true,
                PreparePriceModel = true,
                PrepareSpecificationAttributes = _catalogSettings.ShowSpecAttributeOnCatalogPages,
                ProductThumbPictureSize = productThumbPictureSize,
                Products = products,
            });

            return View(model);
        }

        #endregion

    }
}
