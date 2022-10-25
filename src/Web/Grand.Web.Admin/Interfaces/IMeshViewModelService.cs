using Grand.Web.Admin.Models.Catalog;
using static Grand.Web.Admin.Models.Catalog.ProductModel;

namespace Grand.Web.Admin.Interfaces
{
    public interface IMeshViewModelService
    {
        // Convert over ProductAttributeMappingModel to Material Model
        Task<IList<MaterialModel>> PrepareMeshViewModel(ProductAttributeMappingModel model);
    }
}
