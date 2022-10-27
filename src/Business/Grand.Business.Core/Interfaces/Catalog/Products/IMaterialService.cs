using Grand.Domain.Catalog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Business.Core.Interfaces.Catalog.Products
{
    public interface IMaterialService
    {
        Task<Material> UpdateMaterial(Material material, string productId, string productAttributeMappingId, string productAttributeValueId);

        Task<Material> InsertMaterial(Material material, string productId, string productAttributeMappingId, string productAttributeValueId);
    }
}
