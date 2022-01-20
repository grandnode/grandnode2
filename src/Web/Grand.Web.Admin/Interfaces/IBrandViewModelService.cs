using Grand.Domain.Catalog;
using Grand.Web.Admin.Models.Catalog;

namespace Grand.Web.Admin.Interfaces
{
    public interface IBrandViewModelService
    {
        void PrepareSortOptionsModel(BrandModel model);
        Task PrepareLayoutsModel(BrandModel model);
        Task PrepareDiscountModel(BrandModel model, Brand brand, bool excludeProperties);
        Task<Brand> InsertBrandModel(BrandModel model);
        Task<Brand> UpdateBrandModel(Brand brand, BrandModel model);
        Task DeleteBrand(Brand brand);
        Task<(IEnumerable<BrandModel.ActivityLogModel> activityLogModels, int totalCount)> PrepareActivityLogModel(string brandId, int pageIndex, int pageSize);
    }
}
