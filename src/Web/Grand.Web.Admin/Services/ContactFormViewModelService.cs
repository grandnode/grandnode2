using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Marketing.Contacts;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Domain.Messages;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Messages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Services;

public class ContactFormViewModelService : IContactFormViewModelService
{
    private readonly IContactUsService _contactUsService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IEmailAccountService _emailAccountService;
    private readonly IStoreService _storeService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContext _workContext;

    public ContactFormViewModelService(IContactUsService contactUsService,
        IDateTimeService dateTimeService,
        ITranslationService translationService,
        IWorkContext workContext,
        IStoreService storeService,
        IEmailAccountService emailAccountService)
    {
        _contactUsService = contactUsService;
        _dateTimeService = dateTimeService;
        _translationService = translationService;
        _workContext = workContext;
        _storeService = storeService;
        _emailAccountService = emailAccountService;
    }

    public virtual async Task<ContactFormListModel> PrepareContactFormListModel()
    {
        var model = new ContactFormListModel();
        //stores
        model.AvailableStores.Add(new SelectListItem
            { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });
        foreach (var s in await _storeService.GetAllStores())
            model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id });

        return model;
    }

    public virtual async Task<(IEnumerable<ContactFormModel> contactFormModel, int totalCount)>
        PrepareContactFormListModel(ContactFormListModel model, int pageIndex, int pageSize)
    {
        DateTime? startDateValue = model.SearchStartDate == null
            ? null
            : _dateTimeService.ConvertToUtcTime(model.SearchStartDate.Value, _dateTimeService.CurrentTimeZone);

        DateTime? endDateValue = model.SearchEndDate == null
            ? null
            : _dateTimeService.ConvertToUtcTime(model.SearchEndDate.Value, _dateTimeService.CurrentTimeZone).AddDays(1);

        var contactform = await _contactUsService.GetAllContactUs(
            startDateValue,
            endDateValue,
            model.SearchEmail,
            storeId: model.StoreId,
            pageIndex: pageIndex - 1,
            pageSize: pageSize);
        var contactformmodelList = new List<ContactFormModel>();
        foreach (var item in contactform)
        {
            var store = await _storeService.GetStoreById(item.StoreId);
            var m = item.ToModel();
            m.CreatedOn = _dateTimeService.ConvertToUserTime(item.CreatedOnUtc, DateTimeKind.Utc);
            m.Enquiry = "";
            m.Email = m.FullName + " - " + m.Email;
            m.Store = store != null ? store.Shortcut : "-empty-";
            contactformmodelList.Add(m);
        }

        return (contactformmodelList, contactform.TotalCount);
    }

    public virtual async Task<ContactFormModel> PrepareContactFormModel(ContactUs contactUs)
    {
        var model = contactUs.ToModel();
        model.CreatedOn = _dateTimeService.ConvertToUserTime(contactUs.CreatedOnUtc, DateTimeKind.Utc);
        var store = await _storeService.GetStoreById(contactUs.StoreId);
        model.Store = store != null ? store.Shortcut : "-empty-";
        var email = await _emailAccountService.GetEmailAccountById(contactUs.EmailAccountId);
        model.EmailAccountName = email != null ? email.DisplayName : "-empty-";
        return model;
    }
}