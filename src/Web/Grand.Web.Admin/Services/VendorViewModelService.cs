using Grand.Business.Core.Events.Customers;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Directory;
using Grand.Domain.Discounts;
using Grand.Domain.Seo;
using Grand.Domain.Vendors;
using Grand.SharedKernel.Extensions;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Common;
using Grand.Web.Admin.Models.Vendors;
using Grand.Web.Common.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Services;

public class VendorViewModelService(
    IDiscountService discountService,
    IVendorService vendorService,
    ICustomerService customerService,
    ITranslationService translationService,
    IDateTimeService dateTimeService,
    ICountryService countryService,
    IStoreService storeService,
    ISlugService slugService,
    IPictureService pictureService,
    IMediator mediator,
    VendorSettings vendorSettings,
    ILanguageService languageService,
    SeoSettings seoSettings)
    : IVendorViewModelService
{
    public virtual async Task PrepareDiscountModel(VendorModel model, Vendor vendor, bool excludeProperties)
    {
        ArgumentNullException.ThrowIfNull(model);

        model.AvailableDiscounts = (await discountService
                .GetDiscountsQuery(DiscountType.AssignedToVendors))
            .Select(d => d.ToModel(dateTimeService))
            .ToList();

        if (!excludeProperties && vendor != null) model.SelectedDiscountIds = vendor.AppliedDiscounts.ToArray();
    }

    public virtual async Task PrepareVendorReviewModel(VendorReviewModel model,
        VendorReview vendorReview, bool excludeProperties, bool formatReviewText)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(vendorReview);

        var vendor = await vendorService.GetVendorById(vendorReview.VendorId);
        var customer = await customerService.GetCustomerById(vendorReview.CustomerId);

        model.Id = vendorReview.Id;
        model.VendorId = vendorReview.VendorId;
        model.VendorName = vendor.Name;
        model.CustomerId = vendorReview.CustomerId;
        model.CustomerInfo = customer != null
            ? !string.IsNullOrEmpty(customer.Email)
                ? customer.Email
                : translationService.GetResource("Admin.Customers.Guest")
            : "";
        model.Rating = vendorReview.Rating;
        model.CreatedOn = dateTimeService.ConvertToUserTime(vendorReview.CreatedOnUtc, DateTimeKind.Utc);
        if (!excludeProperties)
        {
            model.Title = vendorReview.Title;
            model.ReviewText = formatReviewText
                ? FormatText.ConvertText(vendorReview.ReviewText)
                : vendorReview.ReviewText;
            model.IsApproved = vendorReview.IsApproved;
        }
    }

    public virtual async Task PrepareVendorAddressModel(VendorModel model, Vendor vendor)
    {
        model.Address ??= new AddressModel();

        model.Address.FirstNameEnabled = false;
        model.Address.FirstNameRequired = false;
        model.Address.LastNameEnabled = false;
        model.Address.LastNameRequired = false;
        model.Address.EmailEnabled = false;
        model.Address.EmailRequired = false;
        model.Address.CompanyEnabled = vendorSettings.AddressSettings.CompanyEnabled;
        model.Address.CountryEnabled = vendorSettings.AddressSettings.CountryEnabled;
        model.Address.StateProvinceEnabled = vendorSettings.AddressSettings.StateProvinceEnabled;
        model.Address.CityEnabled = vendorSettings.AddressSettings.CityEnabled;
        model.Address.CityRequired = vendorSettings.AddressSettings.CityRequired;
        model.Address.StreetAddressEnabled = vendorSettings.AddressSettings.StreetAddressEnabled;
        model.Address.StreetAddressRequired = vendorSettings.AddressSettings.StreetAddressRequired;
        model.Address.StreetAddress2Enabled = vendorSettings.AddressSettings.StreetAddress2Enabled;
        model.Address.ZipPostalCodeEnabled = vendorSettings.AddressSettings.ZipPostalCodeEnabled;
        model.Address.ZipPostalCodeRequired = vendorSettings.AddressSettings.ZipPostalCodeRequired;
        model.Address.PhoneEnabled = vendorSettings.AddressSettings.PhoneEnabled;
        model.Address.PhoneRequired = vendorSettings.AddressSettings.PhoneRequired;
        model.Address.FaxEnabled = vendorSettings.AddressSettings.FaxEnabled;
        model.Address.FaxRequired = vendorSettings.AddressSettings.FaxRequired;
        model.Address.NoteEnabled = vendorSettings.AddressSettings.NoteEnabled;
        model.Address.AddressTypeEnabled = false;

        //address
        model.Address.AvailableCountries.Add(new SelectListItem
            { Text = translationService.GetResource("Admin.Address.SelectCountry"), Value = "" });
        foreach (var c in await countryService.GetAllCountries(showHidden: true))
            model.Address.AvailableCountries.Add(new SelectListItem
                { Text = c.Name, Value = c.Id, Selected = vendor != null && c.Id == vendor.Address.CountryId });

        var states = !string.IsNullOrEmpty(model.Address.CountryId)
            ? (await countryService.GetCountryById(model.Address.CountryId))?.StateProvinces
            : new List<StateProvince>();
        if (states.Count > 0)
            foreach (var s in states)
                model.Address.AvailableStates.Add(new SelectListItem {
                    Text = s.Name, Value = s.Id, Selected = vendor != null && s.Id == vendor.Address.StateProvinceId
                });
    }

    public virtual async Task PrepareStore(VendorModel model)
    {
        model.AvailableStores.Add(new SelectListItem {
            Text = "[None]",
            Value = ""
        });

        foreach (var s in await storeService.GetAllStores())
            model.AvailableStores.Add(new SelectListItem {
                Text = s.Shortcut,
                Value = s.Id
            });
    }

    public virtual async Task<VendorModel> PrepareVendorModel()
    {
        var model = new VendorModel();
        //discounts
        await PrepareDiscountModel(model, null, true);
        //default values
        model.PageSize = 6;
        model.Active = true;
        model.AllowCustomersToSelectPageSize = true;
        model.PageSizeOptions = vendorSettings.DefaultVendorPageSizeOptions;

        //default value
        model.Active = true;

        //stores
        await PrepareStore(model);

        //prepare address model
        await PrepareVendorAddressModel(model, null);
        return model;
    }

    public virtual async Task<IList<VendorModel.AssociatedCustomerInfo>> AssociatedCustomers(string vendorId)
    {
        return (await customerService
                .GetAllCustomers(vendorId: vendorId))
            .Select(c => new VendorModel.AssociatedCustomerInfo {
                Id = c.Id,
                Email = c.Email
            })
            .ToList();
    }

    public virtual async Task<Vendor> InsertVendorModel(VendorModel model)
    {
        var vendor = model.ToEntity();
        vendor.Address = model.Address.ToEntity();
        await vendorService.InsertVendor(vendor);

        //discounts
        var allDiscounts = await discountService.GetDiscountsQuery(DiscountType.AssignedToVendors);
        foreach (var discount in allDiscounts)
            if (model.SelectedDiscountIds != null && model.SelectedDiscountIds.Contains(discount.Id))
                vendor.AppliedDiscounts.Add(discount.Id);

        //search engine name
        model.SeName =
            await vendor.ValidateSeName(model.SeName, vendor.Name, true, seoSettings, slugService, languageService);
        vendor.Locales =
            await model.Locales.ToTranslationProperty(vendor, x => x.Name, seoSettings, slugService, languageService);
        vendor.SeName = model.SeName;
        await vendorService.UpdateVendor(vendor);

        //update picture seo file name
        await pictureService.UpdatePictureSeoNames(vendor.PictureId, vendor.Name);
        await slugService.SaveSlug(vendor, model.SeName, "");

        return vendor;
    }

    public virtual async Task<Vendor> UpdateVendorModel(Vendor vendor, VendorModel model)
    {
        var prevPictureId = vendor.PictureId;
        vendor = model.ToEntity(vendor);
        vendor.Locales =
            await model.Locales.ToTranslationProperty(vendor, x => x.Name, seoSettings, slugService, languageService);
        model.SeName =
            await vendor.ValidateSeName(model.SeName, vendor.Name, true, seoSettings, slugService, languageService);
        vendor.Address = model.Address.ToEntity(vendor.Address);

        //discounts
        var allDiscounts = await discountService.GetDiscountsQuery(DiscountType.AssignedToVendors);
        foreach (var discount in allDiscounts)
            if (model.SelectedDiscountIds != null && model.SelectedDiscountIds.Contains(discount.Id))
            {
                //new discount
                if (vendor.AppliedDiscounts.Count(d => d == discount.Id) == 0)
                    vendor.AppliedDiscounts.Add(discount.Id);
            }
            else
            {
                //remove discount
                if (vendor.AppliedDiscounts.Count(d => d == discount.Id) > 0)
                    vendor.AppliedDiscounts.Remove(discount.Id);
            }

        vendor.SeName = model.SeName;

        await vendorService.UpdateVendor(vendor);
        //search engine name                
        await slugService.SaveSlug(vendor, model.SeName, "");

        //delete an old picture (if deleted or updated)
        if (!string.IsNullOrEmpty(prevPictureId) && prevPictureId != vendor.PictureId)
        {
            var prevPicture = await pictureService.GetPictureById(prevPictureId);
            if (prevPicture != null)
                await pictureService.DeletePicture(prevPicture);
        }

        //update picture seo file name
        await pictureService.UpdatePictureSeoNames(vendor.PictureId, vendor.Name);
        return vendor;
    }

    public virtual async Task DeleteVendor(Vendor vendor)
    {
        //clear associated customer references
        var associatedCustomers = await customerService.GetAllCustomers(vendorId: vendor.Id);
        foreach (var customer in associatedCustomers)
        {
            customer.VendorId = "";
            await customerService.UpdateCustomer(customer);
        }

        await vendorService.DeleteVendor(vendor);
    }

    public virtual IList<VendorModel.VendorNote> PrepareVendorNote(Vendor vendor)
    {
        var vendorNoteModels = new List<VendorModel.VendorNote>();
        foreach (var vendorNote in vendor.VendorNotes
                     .OrderByDescending(vn => vn.CreatedOnUtc))
            vendorNoteModels.Add(new VendorModel.VendorNote {
                Id = vendorNote.Id,
                VendorId = vendor.Id,
                Note = vendorNote.Note,
                CreatedOn = dateTimeService.ConvertToUserTime(vendorNote.CreatedOnUtc, DateTimeKind.Utc)
            });
        return vendorNoteModels;
    }

    public virtual async Task<bool> InsertVendorNote(string vendorId, string message)
    {
        var vendor = await vendorService.GetVendorById(vendorId);
        if (vendor == null)
            return false;

        var vendorNote = new VendorNote {
            Note = message,
            CreatedOnUtc = DateTime.UtcNow
        };
        vendor.VendorNotes.Add(vendorNote);
        await vendorService.UpdateVendor(vendor);

        return true;
    }

    public virtual async Task DeleteVendorNote(string id, string vendorId)
    {
        var vendor = await vendorService.GetVendorById(vendorId);
        if (vendor == null)
            throw new ArgumentException("No vendor found with the specified id");

        var vendorNote = vendor.VendorNotes.FirstOrDefault(vn => vn.Id == id);
        if (vendorNote == null)
            throw new ArgumentException("No vendor note found with the specified id");
        await vendorService.DeleteVendorNote(vendorNote, vendorId);
    }

    public virtual async Task<(IEnumerable<VendorReviewModel> vendorReviewModels, int totalCount)>
        PrepareVendorReviewModel(VendorReviewListModel model, int pageIndex, int pageSize)
    {
        DateTime? createdOnFromValue = model.CreatedOnFrom == null
            ? null
            : dateTimeService.ConvertToUtcTime(model.CreatedOnFrom.Value, dateTimeService.CurrentTimeZone);

        DateTime? createdToFromValue = model.CreatedOnTo == null
            ? null
            : dateTimeService.ConvertToUtcTime(model.CreatedOnTo.Value, dateTimeService.CurrentTimeZone).AddDays(1);

        var vendorReviews = await vendorService.GetAllVendorReviews("", null,
            createdOnFromValue, createdToFromValue, model.SearchText, model.SearchVendorId, pageIndex - 1, pageSize);
        var items = new List<VendorReviewModel>();
        foreach (var x in vendorReviews)
        {
            var m = new VendorReviewModel();
            await PrepareVendorReviewModel(m, x, false, true);
            items.Add(m);
        }

        return (items, vendorReviews.TotalCount);
    }

    public virtual async Task<VendorReview> UpdateVendorReviewModel(VendorReview vendorReview, VendorReviewModel model)
    {
        vendorReview.Title = model.Title;
        vendorReview.ReviewText = model.ReviewText;
        vendorReview.IsApproved = model.IsApproved;

        await vendorService.UpdateVendorReview(vendorReview);

        var vendor = await vendorService.GetVendorById(vendorReview.VendorId);
        //update vendor totals
        await vendorService.UpdateVendorReviewTotals(vendor);
        return vendorReview;
    }

    public virtual async Task DeleteVendorReview(VendorReview vendorReview)
    {
        await vendorService.DeleteVendorReview(vendorReview);
        var vendor = await vendorService.GetVendorById(vendorReview.VendorId);
        //update vendor totals
        await vendorService.UpdateVendorReviewTotals(vendor);
    }

    public virtual async Task ApproveVendorReviews(IEnumerable<string> selectedIds)
    {
        foreach (var id in selectedIds)
        {
            var idReview = id.Split(':').First();
            var idVendor = id.Split(':').Last();
            var vendor = await vendorService.GetVendorById(idVendor);
            var vendorReview = await vendorService.GetVendorReviewById(idReview);
            if (vendorReview != null)
            {
                var previousIsApproved = vendorReview.IsApproved;
                vendorReview.IsApproved = true;
                await vendorService.UpdateVendorReview(vendorReview);
                await vendorService.UpdateVendorReviewTotals(vendor);

                //raise event (only if it wasn't approved before)
                if (!previousIsApproved)
                    await mediator.Publish(new VendorReviewApprovedEvent(vendorReview));
            }
        }
    }

    public virtual async Task DisapproveVendorReviews(IEnumerable<string> selectedIds)
    {
        foreach (var id in selectedIds)
        {
            var idReview = id.Split(':').First();
            var idVendor = id.Split(':').Last();

            var vendor = await vendorService.GetVendorById(idVendor);
            var vendorReview = await vendorService.GetVendorReviewById(idReview);
            if (vendorReview != null)
            {
                vendorReview.IsApproved = false;
                await vendorService.UpdateVendorReview(vendorReview);
                await vendorService.UpdateVendorReviewTotals(vendor);
            }
        }
    }
}