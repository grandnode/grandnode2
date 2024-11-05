using Grand.Domain.Tax;

namespace Grand.Business.System.Services.Installation;

public partial class InstallationService
{
    protected virtual Task InstallTaxCategories()
    {
        var taxCategories = new List<TaxCategory> {
            new() {
                Name = "Lego",
                DisplayOrder = 1
            },
            new() {
                Name = "Electronics & Software",
                DisplayOrder = 5
            },
            new() {
                Name = "Downloadable Products",
                DisplayOrder = 10
            },
            new() {
                Name = "Balls",
                DisplayOrder = 15
            },
            new() {
                Name = "Apparel",
                DisplayOrder = 20
            }
        };
        taxCategories.ForEach(x => _taxCategoryRepository.Insert(x));
        return Task.CompletedTask;
    }
}