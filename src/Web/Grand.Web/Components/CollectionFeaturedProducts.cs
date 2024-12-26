﻿using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Features.Models.Catalog;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class CollectionFeaturedProductsViewComponent : BaseViewComponent
{
    #region Constructors

    public CollectionFeaturedProductsViewComponent(IMediator mediator, IWorkContextAccessor workContextAccessor)
    {
        _mediator = mediator;
        _workContext = workContextAccessor.WorkContext;
    }

    #endregion

    #region Invoker

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var model = await _mediator.Send(new GetCollectionFeaturedProducts {
            Customer = _workContext.CurrentCustomer,
            Language = _workContext.WorkingLanguage,
            Store = _workContext.CurrentStore
        });

        return !model.Any() ? Content("") : View(model);
    }

    #endregion

    #region Fields

    private readonly IMediator _mediator;
    private readonly IWorkContext _workContext;

    #endregion
}