using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Marketing.Newsletters;
using Grand.Business.Core.Utilities.Common.Security;
using Grand.Infrastructure;
using Grand.SharedKernel.Extensions;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Models.Messages;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;

namespace Grand.Web.Admin.Controllers;

[PermissionAuthorize(PermissionSystemName.NewsletterSubscribers)]
public class NewsLetterSubscriptionController : BaseAdminController
{
    private readonly IDateTimeService _dateTimeService;
    private readonly IGroupService _groupService;
    private readonly INewsletterCategoryService _newsletterCategoryService;
    private readonly INewsLetterSubscriptionService _newsLetterSubscriptionService;
    private readonly IStoreService _storeService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContext _workContext;

    public NewsLetterSubscriptionController(INewsLetterSubscriptionService newsLetterSubscriptionService,
        INewsletterCategoryService newsletterCategoryService,
        IDateTimeService dateTimeService,
        ITranslationService translationService,
        IStoreService storeService,
        IGroupService groupService,
        IWorkContext workContext)
    {
        _newsLetterSubscriptionService = newsLetterSubscriptionService;
        _newsletterCategoryService = newsletterCategoryService;
        _dateTimeService = dateTimeService;
        _translationService = translationService;
        _storeService = storeService;
        _groupService = groupService;
        _workContext = workContext;
    }

    [NonAction]
    protected virtual async Task<string> GetCategoryNames(IList<string> categoryNames, string separator = ",")
    {
        var sb = new StringBuilder();
        for (var i = 0; i < categoryNames.Count; i++)
        {
            var category = await _newsletterCategoryService.GetNewsletterCategoryById(categoryNames[i]);
            if (category != null)
            {
                sb.Append(category.Name);
                if (i != categoryNames.Count - 1)
                {
                    sb.Append(separator);
                    sb.Append(" ");
                }
            }
        }

        return sb.ToString();
    }


    public IActionResult Index()
    {
        return RedirectToAction("List");
    }

    public async Task<IActionResult> List()
    {
        var model = new NewsLetterSubscriptionListModel();

        var storeId = _workContext.CurrentCustomer.StaffStoreId;

        //stores
        model.AvailableStores.Add(new SelectListItem
            { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
        foreach (var s in (await _storeService.GetAllStores()).Where(x =>
                     x.Id == storeId || string.IsNullOrWhiteSpace(storeId)))
            model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id });

        //active
        model.ActiveList.Add(new SelectListItem {
            Value = " ",
            Text = _translationService.GetResource("admin.marketing.NewsLetterSubscriptions.List.SearchActive.All")
        });
        model.ActiveList.Add(new SelectListItem {
            Value = "1",
            Text = _translationService.GetResource(
                "admin.marketing.NewsLetterSubscriptions.List.SearchActive.ActiveOnly")
        });
        model.ActiveList.Add(new SelectListItem {
            Value = "2",
            Text = _translationService.GetResource(
                "admin.marketing.NewsLetterSubscriptions.List.SearchActive.NotActiveOnly")
        });

        foreach (var ca in await _newsletterCategoryService.GetAllNewsletterCategory())
            model.AvailableCategories.Add(new SelectListItem { Text = ca.Name, Value = ca.Id });

        return View(model);
    }

    [PermissionAuthorizeAction(PermissionActionName.List)]
    [HttpPost]
    public async Task<IActionResult> SubscriptionList(DataSourceRequest command, NewsLetterSubscriptionListModel model,
        string[] searchCategoryIds)
    {
        bool? isActive = null;
        switch (model.ActiveId)
        {
            case 1:
                isActive = true;
                break;
            case 2:
                isActive = false;
                break;
        }

        if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            model.StoreId = _workContext.CurrentCustomer.StaffStoreId;

        var newsletterSubscriptions = await _newsLetterSubscriptionService.GetAllNewsLetterSubscriptions(
            model.SearchEmail,
            model.StoreId, isActive, searchCategoryIds, command.Page - 1, command.PageSize);
        var items = new List<NewsLetterSubscriptionModel>();
        foreach (var x in newsletterSubscriptions)
        {
            var m = x.ToModel();
            var store = await _storeService.GetStoreById(x.StoreId);
            m.StoreName = store != null ? store.Shortcut : "Unknown store";
            m.CreatedOn = _dateTimeService.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc)
                .ToString(CultureInfo.InvariantCulture);
            m.Categories = await GetCategoryNames(x.Categories.ToList());
            items.Add(m);
        }

        var gridModel = new DataSourceResult {
            Data = items,
            Total = newsletterSubscriptions.TotalCount
        };

        return Json(gridModel);
    }

    [PermissionAuthorizeAction(PermissionActionName.Edit)]
    [HttpPost]
    public async Task<IActionResult> SubscriptionUpdate(NewsLetterSubscriptionModel model)
    {
        if (!ModelState.IsValid) return Json(new DataSourceResult { Errors = ModelState.SerializeErrors() });

        var subscription = await _newsLetterSubscriptionService.GetNewsLetterSubscriptionById(model.Id);
        subscription.Email = model.Email;
        subscription.Active = model.Active;
        await _newsLetterSubscriptionService.UpdateNewsLetterSubscription(subscription);

        return new JsonResult("");
    }

    [PermissionAuthorizeAction(PermissionActionName.Delete)]
    [HttpPost]
    public async Task<IActionResult> SubscriptionDelete(string id)
    {
        var subscription = await _newsLetterSubscriptionService.GetNewsLetterSubscriptionById(id);
        if (subscription == null)
            throw new ArgumentException("No subscription found with the specified id");
        await _newsLetterSubscriptionService.DeleteNewsLetterSubscription(subscription);

        return new JsonResult("");
    }

    [PermissionAuthorizeAction(PermissionActionName.Export)]
    [HttpPost]
    public async Task<IActionResult> ExportCsv(NewsLetterSubscriptionListModel model, string[] searchCategoryIds)
    {
        bool? isActive = null;
        switch (model.ActiveId)
        {
            case 1:
                isActive = true;
                break;
            case 2:
                isActive = false;
                break;
        }

        if (await _groupService.IsStaff(_workContext.CurrentCustomer))
            model.StoreId = _workContext.CurrentCustomer.StaffStoreId;

        var subscriptions = await _newsLetterSubscriptionService.GetAllNewsLetterSubscriptions(model.SearchEmail,
            model.StoreId, isActive, searchCategoryIds);

        var result = _newsLetterSubscriptionService.ExportNewsletterSubscribersToTxt(subscriptions);

        var fileName =
            $"newsletter_emails_{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}_{CommonHelper.GenerateRandomDigitCode(4)}.txt";
        return File(Encoding.UTF8.GetBytes(result), "text/csv", fileName);
    }

    [PermissionAuthorizeAction(PermissionActionName.Import)]
    [HttpPost]
    public async Task<IActionResult> ImportCsv(IFormFile importcsvfile)
    {
        try
        {
            if (importcsvfile is { Length: > 0 })
            {
                var count = await _newsLetterSubscriptionService.ImportNewsletterSubscribersFromTxt(
                    importcsvfile.OpenReadStream(), _workContext.CurrentStore.Id);
                Success(string.Format(
                    _translationService.GetResource("admin.marketing.NewsLetterSubscriptions.ImportEmailsSuccess"),
                    count));
                return RedirectToAction("List");
            }

            Error(_translationService.GetResource("Admin.Common.UploadFile"));
            return RedirectToAction("List");
        }
        catch (Exception exc)
        {
            Error(exc);
            return RedirectToAction("List");
        }
    }
}