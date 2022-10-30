using Grand.Domain.Catalog;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Interfaces;
using static Grand.Web.Admin.Models.Catalog.ProductModel;

namespace Grand.Web.Admin.Services
{
    public class MaterialViewModelService: IMaterialViewModelService
    {

        public IList<MaterialModel> PrepareMaterialViewModel(IEnumerable<Material> materials)
        {
            var materialsList = new List<MaterialModel>();
            
            foreach (var material in materials)
            {
                materialsList.Add(material.ToModel());
            }

            return materialsList;
        }
    }
}
