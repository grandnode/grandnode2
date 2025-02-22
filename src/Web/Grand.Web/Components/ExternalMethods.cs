using Grand.Business.Core.Interfaces.Authentication;
using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Models.Customer;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Components;

public class ExternalMethodsViewComponent : BaseViewComponent
{
    private readonly IExternalAuthenticationService _externalAuthenticationService;
    private readonly IContextAccessor _contextAccessor;

    public ExternalMethodsViewComponent(
        IExternalAuthenticationService externalAuthenticationService,
        IContextAccessor contextAccessor)
    {
        _externalAuthenticationService = externalAuthenticationService;
        _contextAccessor = contextAccessor;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var models = _externalAuthenticationService
            .LoadActiveAuthenticationProviders(_contextAccessor.WorkContext.CurrentCustomer, _contextAccessor.StoreContext.CurrentStore);

        var model = new List<ExternalAuthenticationMethodModel>();
        foreach (var item in models)
        {
            var viewComponentName = await item.GetPublicViewComponentName();
            model.Add(new ExternalAuthenticationMethodModel {
                ViewComponentName = viewComponentName
            });
        }

        return View(model);
    }
}