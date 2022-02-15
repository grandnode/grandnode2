﻿using Grand.Web.Commands.Models.Newsletter;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.Filters;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Controllers
{
    [DenySystemAccount]
    public partial class NewsletterController : BasePublicController
    {
        private readonly IMediator _mediator;

        public NewsletterController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost]
        public virtual async Task<IActionResult> SubscribeNewsletter(string email, bool subscribe)
        {
            var model = await _mediator.Send(new SubscribeNewsletterCommand() { Email = email, Subscribe = subscribe });
            if (model.NewsletterCategory != null)
            {
                model.ShowCategories = true;
                model.ResultCategory = await this.RenderPartialViewToString("NewsletterCategory", model.NewsletterCategory, true);
            }
            return Json(new
            {
                Success = model.Success,
                Result = model.Result,
                Showcategories = model.ShowCategories,
                ResultCategory = model.ResultCategory,
            });
        }

        [HttpPost]
        public virtual async Task<IActionResult> SaveCategories(IFormCollection form)
        {
            var model = await _mediator.Send(new SubscriptionCategoryCommand() { Values = form.Keys.ToDictionary(k => k, v => Request.Form[v].ToString()) });
            return Json(new
            {
                Success = model.success,
                Message = model.message
            });
        }


        public virtual async Task<IActionResult> SubscriptionActivation(Guid token, bool active)
        {
            var model = await _mediator.Send(new SubscriptionActivationCommand() { Active = active, Token = token });
            if (model == null)
                return RedirectToRoute("HomePage");

            return View(model);
        }
    }
}
