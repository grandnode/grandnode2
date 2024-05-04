using Grand.Web.Commands.Models.Newsletter;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using Grand.Web.Models.Newsletter;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Controllers;

[DenySystemAccount]
public class NewsletterController : BasePublicController
{
    private readonly IMediator _mediator;

    public NewsletterController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public virtual async Task<IActionResult> SubscribeNewsletter(string email, bool subscribe)
    {
        var model = await _mediator.Send(new SubscribeNewsletterCommand { Email = email, Subscribe = subscribe });
        if (model.NewsletterCategory == null)
            return Json(new {
                model.Success,
                model.Result,
                Showcategories = model.ShowCategories,
                model.ResultCategory
            });
        model.ShowCategories = true;
        model.ResultCategory =
            await this.RenderPartialViewToString("NewsletterCategory", model.NewsletterCategory, true);
        return Json(new {
            model.Success,
            model.Result,
            Showcategories = model.ShowCategories,
            model.ResultCategory
        });
    }

    [HttpPost]
    public virtual async Task<IActionResult> SaveCategories(NewsletterCategoryModel model)
    {
        var result = await _mediator.Send(new SubscriptionCategoryCommand { Model = model });
        return Json(new {
            Success = result.success,
            Message = result.message
        });
    }

    [HttpGet]
    public virtual async Task<IActionResult> SubscriptionActivation(Guid token, bool active)
    {
        var model = await _mediator.Send(new SubscriptionActivationCommand { Active = active, Token = token });
        return model == null ? RedirectToRoute("HomePage") : View(model);
    }
}