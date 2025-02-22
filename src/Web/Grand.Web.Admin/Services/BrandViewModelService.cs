using Grand.Business.Core.Interfaces.Catalog.Brands;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Seo;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Catalog;
using Grand.Domain.Discounts;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Catalog;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Services;

public class BrandViewModelService : IBrandViewModelService
{
    
    #region Fields

    private readonly IBrandService _brandService;
    private readonly IBrandLayoutService _brandLayoutService;
    private readonly IPictureService _pictureService;
    private readonly IDiscountService _discountService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IContextAccessor _contextAccessor;
    private readonly ISeNameService _seNameService;
    private readonly IEnumTranslationService _enumTranslationService;
    
    #endregion

    #region Constructors

    public BrandViewModelService(
        IBrandService brandService,
        IBrandLayoutService brandLayoutService,
        IPictureService pictureService,
        IDiscountService discountService,
        IDateTimeService dateTimeService,
        IContextAccessor contextAccessor,
        ISeNameService seNameService, 
        IEnumTranslationService enumTranslationService)
    {
        _brandLayoutService = brandLayoutService;
        _brandService = brandService;
        _pictureService = pictureService;
        _discountService = discountService;
        _dateTimeService = dateTimeService;
        _contextAccessor = contextAccessor;
        _seNameService = seNameService;
        _enumTranslationService = enumTranslationService;
    }

    #endregion

    public virtual void PrepareSortOptionsModel(BrandModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        model.AvailableSortOptions = _enumTranslationService.ToSelectList(ProductSortingEnum.Position).ToList();
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
                .GetDiscountsQuery(DiscountType.AssignedToBrands, _contextAccessor.WorkContext.CurrentCustomer.Id))
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
        
        //search engine name
        brand.Locales = await _seNameService.TranslationSeNameProperties(model.Locales, brand, x => x.Name);
        brand.SeName = await _seNameService.ValidateSeName(brand, model.SeName, brand.Name, true);

        await _brandService.InsertBrand(brand);

        //search engine name
        await _seNameService.SaveSeName(brand);

        //update picture seo file name
        await _pictureService.UpdatePictureSeoNames(brand.PictureId, brand.Name);

        return brand;
    }

    public virtual async Task<Brand> UpdateBrandModel(Brand brand, BrandModel model)
    {
        var prevPictureId = brand.PictureId;
        brand = model.ToEntity(brand);
        
        brand.Locales = await _seNameService.TranslationSeNameProperties(model.Locales, brand, x => x.Name);
        brand.SeName = await _seNameService.ValidateSeName(brand, model.SeName, brand.Name, true);
        
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

        //update brand
        await _brandService.UpdateBrand(brand);
        
        //search engine name
        await _seNameService.SaveSeName(brand);

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
}