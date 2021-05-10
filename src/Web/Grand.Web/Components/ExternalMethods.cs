using Grand.Business.Authentication.Interfaces;
using Grand.Infrastructure;
using Grand.Web.Common.Components;
using Grand.Web.Models.Customer;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Web.ViewComponents
{
    public class ExternalMethodsViewComponent : BaseViewComponent
    {
        private readonly IExternalAuthenticationService _externalAuthenticationService;
        private readonly IWorkContext _workContext;

        public ExternalMethodsViewComponent(
            IExternalAuthenticationService externalAuthenticationService,
            IWorkContext workContext)
        {
            _externalAuthenticationService = externalAuthenticationService;
            _workContext = workContext;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var models = _externalAuthenticationService
                .LoadActiveAuthenticationProviders(_workContext.CurrentCustomer, _workContext.CurrentStore);

            var model = new List<ExternalAuthenticationMethodModel>();
            foreach (var item in models)
            {
                var viewComponentName = await item.GetPublicViewComponentName();
                model.Add(new ExternalAuthenticationMethodModel
                {
                    ViewComponentName = viewComponentName
                });
            }

            return View(model);

        }
    }
}