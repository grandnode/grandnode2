using Grand.Domain.Catalog;
using Grand.Web.Admin.Models.Catalog;
using static Grand.Web.Admin.Models.Catalog.ProductModel;

namespace Grand.Web.Admin.Interfaces
{
    public interface IMaterialViewModelService
    {
        IList<MaterialModel> PrepareMaterialViewModel(IEnumerable<Material> materials);

    }
}
