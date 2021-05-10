using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.Catalog;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Components
{
    public class CollectionFeaturedProductsViewComponent : BaseViewComponent
    {
        #region Fields

        private readonly IMediator _mediator;
        private readonly IWorkContext _workContext;

        #endregion

        #region Constructors

        public CollectionFeaturedProductsViewComponent(IMediator mediator, IWorkContext workContext)
        {
            _mediator = mediator;
            _workContext = workContext;
        }

        #endregion

        #region Invoker

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var model = await _mediator.Send(new GetCollectionFeaturedProducts() {
                Customer = _workContext.CurrentCustomer,
                Language = _workContext.WorkingLanguage,
                Store = _workContext.CurrentStore
            });

            if (!model.Any())
                return Content("");
            return View(model);
        }

        #endregion

    }
}
