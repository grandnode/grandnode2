using Grand.Business.System.Interfaces.Installation;
using Grand.Domain.Tax;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Business.System.Services.Installation
{
    public partial class InstallationService : IInstallationService
    {
        protected virtual async Task InstallTaxCategories()
        {
            var taxCategories = new List<TaxCategory>
                               {
                                   new TaxCategory
                                       {
                                           Name = "Lego",
                                           DisplayOrder = 1,
                                       },
                                   new TaxCategory
                                       {
                                           Name = "Electronics & Software",
                                           DisplayOrder = 5,
                                       },
                                   new TaxCategory
                                       {
                                           Name = "Downloadable Products",
                                           DisplayOrder = 10,
                                       },
                                   new TaxCategory
                                       {
                                           Name = "Balls",
                                           DisplayOrder = 15,
                                       },
                                   new TaxCategory
                                       {
                                           Name = "Apparel",
                                           DisplayOrder = 20,
                                       },
                               };
            await _taxCategoryRepository.InsertAsync(taxCategories);

        }
    }
}
