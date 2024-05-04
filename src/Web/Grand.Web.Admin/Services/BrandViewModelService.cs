using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using Grand.Domain.Discounts;
using Grand.Domain.Seo;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Catalog;
using Grand.Web.Common.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Services;

public class BrandViewModelService : IBrandViewModelService
{
    #region Constructors

    public BrandViewModelService(
        IBrandService brandService,
        IBrandLayoutService brandLayoutService,
        ISlugService slugService,
        IPictureService pictureService,
        IDiscountService discountService,
        IDateTimeService dateTimeService,
        ILanguageService languageService,
        IWorkContext workContext,
        SeoSettings seoSettings)
    {
        _brandLayoutService = brandLayoutService;
        _brandService = brandService;
        _slugService = slugService;
        _pictureService = pictureService;
        _discountService = discountService;
        _dateTimeService = dateTimeService;
        _languageService = languageService;
        _workContext = workContext;
        _seoSettings = seoSettings;
    }

    #endregion

    public virtual void PrepareSortOptionsModel(BrandModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        model.AvailableSortOptions = ProductSortingEnum.Position.ToSelectList().ToList();
        model.AvailableSortOptions.Insert(0, new SelectListItem { Text = "None", Value = "-1" });
    }

    public virtual async Task PrepareLayoutsModel(BrandModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var layouts = await _brandLayoutService.GetAllBrandLayouts();
        foreach (var layout in layouts)
            model.AvailableBrandLayouts.Add(new SelectListItem {
                Text = layout.Name,
                Value = layout.Id
            });
    }


    public virtual async Task PrepareDiscountModel(BrandModel model, Brand brand, bool excludeProperties)
    {
        ArgumentNullException.ThrowIfNull(model);

        model.AvailableDiscounts = (await _discountService
                .GetDiscountsQuery(DiscountType.AssignedToBrands, _workContext.CurrentCustomer.Id))
            .Select(d => d.ToModel(_dateTimeService))
            .ToList();

        if (!excludeProperties && brand != null) model.SelectedDiscountIds = brand.AppliedDiscounts.ToArray();
    }

    public virtual async Task<Brand> InsertBrandModel(BrandModel model)
    {
        var brand = model.ToEntity();
        //discounts
        var allDiscounts = await _discountService.GetDiscountsQuery(DiscountType.AssignedToBrands);
        foreach (var discount in allDiscounts)
            if (model.SelectedDiscountIds != null && model.SelectedDiscountIds.Contains(discount.Id))
                brand.AppliedDiscounts.Add(discount.Id);

        await _brandService.InsertBrand(brand);
        //search engine name
        brand.Locales =
            await model.Locales.ToTranslationProperty(brand, x => x.Name, _seoSettings, _slugService, _languageService);
        model.SeName = await brand.ValidateSeName(model.SeName, brand.Name, true, _seoSettings, _slugService,
            _languageService);
        brand.SeName = model.SeName;
        await _brandService.UpdateBrand(brand);

        await _slugService.SaveSlug(brand, model.SeName, "");

        //update picture seo file name
        await _pictureService.UpdatePictureSeoNames(brand.PictureId, brand.Name);

        return brand;
    }

    public virtual async Task<Brand> UpdateBrandModel(Brand brand, BrandModel model)
    {
        var prevPictureId = brand.PictureId;
        brand = model.ToEntity(brand);
        brand.Locales =
            await model.Locales.ToTranslationProperty(brand, x => x.Name, _seoSettings, _slugService, _languageService);
        //discounts
        var allDiscounts = await _discountService.GetDiscountsQuery(DiscountType.AssignedToBrands);
        foreach (var discount in allDiscounts)
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

        model.SeName = await brand.ValidateSeName(model.SeName, brand.Name, true, _seoSettings, _slugService,
            _languageService);
        brand.SeName = model.SeName;

        await _brandService.UpdateBrand(brand);
        //search engine name
        await _slugService.SaveSlug(brand, model.SeName, "");

        //delete an old picture (if deleted or updated)
        if (!string.IsNullOrEmpty(prevPictureId) && prevPictureId != brand.PictureId)
        {
            var prevPicture = await _pictureService.GetPictureById(prevPictureId);
            if (prevPicture != null)
                await _pictureService.DeletePicture(prevPicture);
        }

        //update picture seo file name
        await _pictureService.UpdatePictureSeoNames(brand.PictureId, brand.Name);

        return brand;
    }

    public virtual async Task DeleteBrand(Brand brand)
    {
        await _brandService.DeleteBrand(brand);
    }

    #region Fields

    private readonly IBrandService _brandService;
    private readonly IBrandLayoutService _brandLayoutService;
    private readonly ISlugService _slugService;
    private readonly IPictureService _pictureService;
    private readonly IDiscountService _discountService;
    private readonly IDateTimeService _dateTimeService;
    private readonly ILanguageService _languageService;
    private readonly IWorkContext _workContext;
    private readonly SeoSettings _seoSettings;

    #endregion
}