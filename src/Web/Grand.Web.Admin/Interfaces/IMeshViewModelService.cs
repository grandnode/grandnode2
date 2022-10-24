using Grand.Web.Admin.Models.Catalog;
using static Grand.Web.Admin.Models.Catalog.ProductModel;

namespace Grand.Web.Admin.Interfaces
{
    public interface MeshViewModelService
    {
        Task<IList<MaterialModel>> PrepareMeshViewModel(ProductAttributeMappingModel model);
    }
}
