using Grand.Infrastructure;
using Grand.Domain.Catalog;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.Catalog;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.ViewComponents
{
    public class CollectionNavigationViewComponent : BaseViewComponent
    {
        private readonly IMediator _mediator;
        private readonly IWorkContext _workContext;
        private readonly CatalogSettings _catalogSettings;

        public CollectionNavigationViewComponent(
            IMediator mediator,
            IWorkContext workContext,
            CatalogSettings catalogSettings)
        {
            _mediator = mediator;
            _workContext = workContext;
            _catalogSettings = catalogSettings;
        }

        public async Task<IViewComponentResult> InvokeAsync(string currentCollectionId)
        {
            if (_catalogSettings.CollectionsBlockItemsToDisplay == 0)
                return Content("");

            var model = await _mediator.Send(new GetCollectionNavigation() {
                CurrentCollectionId = currentCollectionId,
                Customer = _workContext.CurrentCustomer,
                Language = _workContext.WorkingLanguage,
                Store = _workContext.CurrentStore
            });

            if (!model.Collections.Any())
                return Content("");

            return View(model);
        }
    }
}