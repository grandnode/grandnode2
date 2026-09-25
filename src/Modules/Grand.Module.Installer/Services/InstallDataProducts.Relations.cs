using Grand.Domain.Catalog;

namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    // Adds slugs, tags, related products, cross-sells, and reviews for all sample products.
    // Bundle items and grouped-child links are added by the department files that own the
    // grouped/bundle products, not here.
    protected virtual Task InstallProductRelations(List<Product> all, SampleProductContext ctx) =>
        Task.CompletedTask;
}
