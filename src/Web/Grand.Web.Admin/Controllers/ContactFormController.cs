using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Marketing.Contacts;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Messages;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

[PermissionAuthorize(PermissionSystemName.MessageContactForm)]
public class ContactFormController : BaseAdminController
{
    private readonly IContactFormViewModelService _contactFormViewModelService;
    private readonly IContactUsService _contactUsService;
    private readonly ITranslationService _translationService;

    public ContactFormController(IContactUsService contactUsService,
        IContactFormViewModelService contactFormViewModelService,
        ITranslationService translationService)
    {
        _contactUsService = contactUsService;
        _contactFormViewModelService = contactFormViewModelService;
        _translationService = translationService;
    }

    public IActionResult Index()
    {
        return RedirectToAction("List");
    }

    public async Task<IActionResult> List()
    {
        var model = await _contactFormViewModelService.PrepareContactFormListModel();
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.List)]
    public async Task<IActionResult> ContactFormList(DataSourceRequest command, ContactFormListModel model)
    {
        var (contactFormModel, totalCount) =
            await _contactFormViewModelService.PrepareContactFormListModel(model, command.Page, command.PageSize);

        var gridModel = new DataSourceResult {
            Data = contactFormModel,
            Total = totalCount
        };
        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Preview)]
    public async Task<IActionResult> Details(string id)
    {
        var contactform = await _contactUsService.GetContactUsById(id);
        if (contactform == null)
            return RedirectToAction("List");

        var model = await _contactFormViewModelService.PrepareContactFormModel(contactform);
        return View(model);
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    public async Task<IActionResult> Delete(ContactFormDeleteModel model)
    {
        var contactForm = await _contactUsService.GetContactUsById(model.Id);
        if (ModelState.IsValid)
        {
            await _contactUsService.DeleteContactUs(contactForm);

            Success(_translationService.GetResource("Admin.System.ContactForm.Deleted"));
            return RedirectToAction("List");
        }

        Error(ModelState);
        return RedirectToAction("Details", new { model.Id });
    }

    [HttpPost]
    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    public async Task<IActionResult> DeleteAll()
    {
        await _contactUsService.ClearTable();

        Success(_translationService.GetResource("Admin.System.ContactForm.DeletedAll"));
        return RedirectToAction("List");
    }
}