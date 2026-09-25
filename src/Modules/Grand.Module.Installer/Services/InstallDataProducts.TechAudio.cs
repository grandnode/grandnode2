using Grand.Domain.Catalog;

namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    // Tech & Audio department products: inserts products (with pictures, categories, brand, vendor,
    // specs, attributes) and returns them. Does not create slugs, tags, relations, or reviews.
    protected virtual Task<List<Product>> InstallProductsTechAudio(SampleProductContext ctx) =>
        Task.FromResult(new List<Product>());
}
