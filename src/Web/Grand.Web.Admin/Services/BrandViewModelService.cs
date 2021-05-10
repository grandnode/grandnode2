using Grand.Business.Catalog.Interfaces.Brands;
using Grand.Business.Catalog.Interfaces.Discounts;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Interfaces.Seo;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Storage.Interfaces;
using Grand.Domain.Catalog;
using Grand.Domain.Discounts;
using Grand.Domain.Seo;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Catalog;
using Grand.Web.Common.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Services
{
    public partial class BrandViewModelService : IBrandViewModelService
    {
        #region Fields

        private readonly IBrandService _brandService;
        private readonly IBrandLayoutService _brandLayoutService;
        private readonly ICustomerService _customerService;
        private readonly ISlugService _slugService;
        private readonly IPictureService _pictureService;
        private readonly ITranslationService _translationService;
        private readonly IDiscountService _discountService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IDateTimeService _dateTimeService;
        private readonly ILanguageService _languageService;
        private readonly IWorkContext _workContext;
        private readonly SeoSettings _seoSettings;

        #endregion

        #region Constructors

        public BrandViewModelService(
            IBrandService brandService,
            IBrandLayoutService brandLayoutService,
            ICustomerService customerService,
            ISlugService slugService,
            IPictureService pictureService,
            ITranslationService translationService,
            IDiscountService discountService,
            ICustomerActivityService customerActivityService,
            IDateTimeService dateTimeService,
            ILanguageService languageService,
            IWorkContext workContext,
            SeoSettings seoSettings)
        {
            _brandLayoutService = brandLayoutService;
            _brandService = brandService;
            _customerService = customerService;
            _slugService = slugService;
            _pictureService = pictureService;
            _translationService = translationService;
            _discountService = discountService;
            _customerActivityService = customerActivityService;
            _dateTimeService = dateTimeService;
            _languageService = languageService;
            _workContext = workContext;
            _seoSettings = seoSettings;
        }

        #endregion

        public virtual void PrepareSortOptionsModel(BrandModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            model.AvailableSortOptions = ProductSortingEnum.Position.ToSelectList().ToList();
            model.AvailableSortOptions.Insert(0, new SelectListItem { Text = "None", Value = "-1" });
        }

        public virtual async Task PrepareLayoutsModel(BrandModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            var layouts = await _brandLayoutService.GetAllBrandLayouts();
            foreach (var layout in layouts)
            {
                model.AvailableBrandLayouts.Add(new SelectListItem
                {
                    Text = layout.Name,
                    Value = layout.Id
                });
            }
        }


        public virtual async Task PrepareDiscountModel(BrandModel model, Brand Brand, bool excludeProperties)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            model.AvailableDiscounts = (await _discountService
                .GetAllDiscounts(DiscountType.AssignedToBrands, storeId: _workContext.CurrentCustomer.Id, showHidden: true))
                .Select(d => d.ToModel(_dateTimeService))
                .ToList();

            if (!excludeProperties && Brand != null)
            {
                model.SelectedDiscountIds = Brand.AppliedDiscounts.ToArray();
            }
        }

        public virtual async Task<Brand> InsertBrandModel(BrandModel model)
        {
            var brand = model.ToEntity();
            brand.CreatedOnUtc = DateTime.UtcNow;
            brand.UpdatedOnUtc = DateTime.UtcNow;
            //discounts
            var allDiscounts = await _discountService.GetAllDiscounts(DiscountType.AssignedToBrands, showHidden: true);
            foreach (var discount in allDiscounts)
            {
                if (model.SelectedDiscountIds != null && model.SelectedDiscountIds.Contains(discount.Id))
                    brand.AppliedDiscounts.Add(discount.Id);
            }

            await _brandService.InsertBrand(brand);
            //search engine name
            brand.Locales = await model.Locales.ToTranslationProperty(brand, x => x.Name, _seoSettings, _slugService, _languageService);
            model.SeName = await brand.ValidateSeName(model.SeName, brand.Name, true, _seoSettings, _slugService, _languageService);
            brand.SeName = model.SeName;
            await _brandService.UpdateBrand(brand);

            await _slugService.SaveSlug(brand, model.SeName, "");

            //update picture seo file name
            await _pictureService.UpdatePictureSeoNames(brand.PictureId, brand.Name);

            //activity log
            await _customerActivityService.InsertActivity("AddNewBrand", brand.Id, _translationService.GetResource("ActivityLog.AddNewBrand"), brand.Name);
            return brand;
        }

        public virtual async Task<Brand> UpdateBrandModel(Brand brand, BrandModel model)
        {
            string prevPictureId = brand.PictureId;
            brand = model.ToEntity(brand);
            brand.UpdatedOnUtc = DateTime.UtcNow;
            brand.Locales = await model.Locales.ToTranslationProperty(brand, x => x.Name, _seoSettings, _slugService, _languageService);
            //discounts
            var allDiscounts = await _discountService.GetAllDiscounts(DiscountType.AssignedToBrands, showHidden: true);
            foreach (var discount in allDiscounts)
            {
                if (model.SelectedDiscountIds != null && model.SelectedDiscountIds.Contains(discount.Id))
                {
                    //new discount
                    if (brand.AppliedDiscounts.Count(d => d == discount.Id) == 0)
                        brand.AppliedDiscounts.Add(discount.Id);
                }
                else
                {
                    //remove discount
                    if (brand.AppliedDiscounts.Count(d => d == discount.Id) > 0)
                        brand.AppliedDiscounts.Remove(discount.Id);
                }
            }
            model.SeName = await brand.ValidateSeName(model.SeName, brand.Name, true, _seoSettings, _slugService, _languageService);
            brand.SeName = model.SeName;

            await _brandService.UpdateBrand(brand);
            //search engine name
            await _slugService.SaveSlug(brand, model.SeName, "");

            //delete an old picture (if deleted or updated)
            if (!String.IsNullOrEmpty(prevPictureId) && prevPictureId != brand.PictureId)
            {
                var prevPicture = await _pictureService.GetPictureById(prevPictureId);
                if (prevPicture != null)
                    await _pictureService.DeletePicture(prevPicture);
            }
            //update picture seo file name
            await _pictureService.UpdatePictureSeoNames(brand.PictureId, brand.Name);

            //activity log
            await _customerActivityService.InsertActivity("EditBrand", brand.Id, _translationService.GetResource("ActivityLog.EditBrand"), brand.Name);
            return brand;
        }

        public virtual async Task DeleteBrand(Brand brand)
        {
            await _brandService.DeleteBrand(brand);
            //activity log
            await _customerActivityService.InsertActivity("DeleteBrand", brand.Id, _translationService.GetResource("ActivityLog.DeleteBrand"), brand.Name);
        }

        public virtual async Task<(IEnumerable<BrandModel.ActivityLogModel> activityLogModels, int totalCount)> PrepareActivityLogModel(string brandId, int pageIndex, int pageSize)
        {
            var activityLog = await _customerActivityService.GetBrandActivities(null, null, brandId, pageIndex - 1, pageSize);
            var items = new List<BrandModel.ActivityLogModel>();
            foreach (var x in activityLog)
            {
                var customer = await _customerService.GetCustomerById(x.CustomerId);
                var m = new BrandModel.ActivityLogModel
                {
                    Id = x.Id,
                    ActivityLogTypeName = (await _customerActivityService.GetActivityTypeById(x.ActivityLogTypeId))?.Name,
                    Comment = x.Comment,
                    CreatedOn = _dateTimeService.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc),
                    CustomerId = x.CustomerId,
                    CustomerEmail = customer != null ? customer.Email : "null"
                };
                items.Add(m);
            }
            return (items, activityLog.TotalCount);
        }
    }
}
