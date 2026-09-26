using Grand.Domain.Catalog;
using Grand.Module.Installer.Extensions;

namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    protected virtual async Task InstallProducts(string defaultUserEmail)
    {
        var productLayoutSimple = _productLayoutRepository.Table.FirstOrDefault(pt => pt.Name == "Simple product");
        if (productLayoutSimple == null)
            throw new Exception("Simple product layout could not be loaded");
        var productLayoutGrouped =
            _productLayoutRepository.Table.FirstOrDefault(pt => pt.Name == "Grouped product (with variants)");
        if (productLayoutGrouped == null)
            throw new Exception("Grouped product layout could not be loaded");

        //delivery date
        var deliveryDate = _deliveryDateRepository.Table.FirstOrDefault();
        if (deliveryDate == null)
            throw new Exception("No default deliveryDate could be loaded");

        //default customer/user
        var defaultCustomer = _customerRepository.Table.FirstOrDefault(x => x.Email == defaultUserEmail);
        if (defaultCustomer == null)
            throw new Exception("Cannot load default customer");

        //default store
        var defaultStore = _storeRepository.Table.FirstOrDefault();
        if (defaultStore == null)
            throw new Exception("No default store could be loaded");

        var ctx = new SampleProductContext(
            productLayoutSimple.Id, productLayoutGrouped.Id, deliveryDate.Id, defaultStore.Id, defaultCustomer.Id);

        var allProducts = new List<Product>();
        allProducts.AddRange(await InstallProductsTechAudio(ctx));
        allProducts.AddRange(await InstallProductsHomeLiving(ctx));
        allProducts.AddRange(await InstallProductsFashion(ctx));
        allProducts.AddRange(await InstallProductsOutdoor(ctx));
        allProducts.AddRange(await InstallProductsDigital(ctx));

        await InstallProductRelations(allProducts, ctx);
    }

    // Counts the tag and adds it to the product; the caller saves the product.
    private async Task AddProductTag(Product product, string tag)
    {
        var productTag = _productTagRepository.Table.FirstOrDefault(pt => pt.Name == tag);
        if (productTag == null)
        {
            productTag = new ProductTag {
                Name = tag,
                SeName = SeoExtensions.GenerateSlug(tag, false, false, false)
            };

            await _productTagRepository.InsertAsync(productTag);
        }

        productTag.Count = productTag.Count + 1;
        await _productTagRepository.UpdateAsync(productTag);
        product.ProductTags.Add(productTag.Name);
    }
}
