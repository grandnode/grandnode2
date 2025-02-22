using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.Catalog;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class CategoryFeaturedProductsViewComponent : BaseViewComponent
{
    #region Constructors

    public CategoryFeaturedProductsViewComponent(
        IMediator mediator,
        IContextAccessor contextAccessor)
    {
        _mediator = mediator;
        _contextAccessor = contextAccessor;
    }

    #endregion

    #region Invoker

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = await _mediator.Send(new GetCategoryFeaturedProducts {
            Customer = _contextAccessor.WorkContext.CurrentCustomer,
            Language = _contextAccessor.WorkContext.WorkingLanguage,
            Store = _contextAccessor.StoreContext.CurrentStore
        });

        return !model.Any() ? Content("") : View(model);
    }

    #endregion

    #region Fields

    private readonly IMediator _mediator;
    private readonly IContextAccessor _contextAccessor;

    #endregion
}