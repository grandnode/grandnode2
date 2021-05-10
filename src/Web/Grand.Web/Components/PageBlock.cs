using Grand.Web.Common.Components;
using Grand.Web.Features.Models.Pages;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Grand.Web.Components
{
    public class PageBlockViewComponent : BaseViewComponent
    {
        #region Fields

        private readonly IMediator _mediator;

        #endregion

        #region Constructors

        public PageBlockViewComponent(
            IMediator mediator)
        {
            _mediator = mediator;
        }

        #endregion

        #region Invoker

        public async Task<IViewComponentResult> InvokeAsync(string systemName)
        {
            var model = await _mediator.Send(new GetPageBlock() { SystemName = systemName });
            if (model == null)
                return Content("");

            return View(model);
        }

        #endregion

    }
}
