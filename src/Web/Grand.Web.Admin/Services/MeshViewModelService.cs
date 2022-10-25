using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Catalog;

namespace Grand.Business.Catalog.Services.Products
{
    public class MeshViewModelService : IMeshViewModelService
    {
        Task<IList<ProductModel.MaterialModel>> IMeshViewModelService.PrepareMeshViewModel(ProductModel.ProductAttributeMappingModel model)
        {
            throw new NotImplementedException();
        }
    }
}
 