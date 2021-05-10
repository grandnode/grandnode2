using Grand.Domain.Catalog;
using Grand.Web.Admin.Models.Catalog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Interfaces
{
    public interface IBrandViewModelService
    {
        void PrepareSortOptionsModel(BrandModel model);
        Task PrepareLayoutsModel(BrandModel model);
        Task PrepareDiscountModel(BrandModel model, Brand Brand, bool excludeProperties);
        Task<Brand> InsertBrandModel(BrandModel model);
        Task<Brand> UpdateBrandModel(Brand Brand, BrandModel model);
        Task DeleteBrand(Brand Brand);
        Task<(IEnumerable<BrandModel.ActivityLogModel> activityLogModels, int totalCount)> PrepareActivityLogModel(string BrandId, int pageIndex, int pageSize);
    }
}
