using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Marketing.Interfaces.Contacts;
using Grand.Business.Messages.Interfaces;
using Grand.Infrastructure;
using Grand.Domain.Messages;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Messages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Services
{
    public partial class ContactFormViewModelService : IContactFormViewModelService
    {
        private readonly IContactUsService _contactUsService;
        private readonly IDateTimeService _dateTimeService;
        private readonly ITranslationService _translationService;
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly IEmailAccountService _emailAccountService;

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
            model.AvailableStores.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });
            foreach (var s in await _storeService.GetAllStores())
                model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id.ToString() });

            return model;
        }

        public virtual async Task<(IEnumerable<ContactFormModel> contactFormModel, int totalCount)> PrepareContactFormListModel(ContactFormListModel model, int pageIndex, int pageSize)
        {
            DateTime? startDateValue = (model.SearchStartDate == null) ? null
                            : (DateTime?)_dateTimeService.ConvertToUtcTime(model.SearchStartDate.Value, _dateTimeService.CurrentTimeZone);

            DateTime? endDateValue = (model.SearchEndDate == null) ? null
                            : (DateTime?)_dateTimeService.ConvertToUtcTime(model.SearchEndDate.Value, _dateTimeService.CurrentTimeZone).AddDays(1);

            string vendorId = "";
            if (_workContext.CurrentVendor != null)
            {
                vendorId = _workContext.CurrentVendor.Id;
            }

            var contactform = await _contactUsService.GetAllContactUs(
                fromUtc: startDateValue,
                toUtc: endDateValue,
                email: model.SearchEmail,
                storeId: model.StoreId,
                vendorId: vendorId,
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
}
