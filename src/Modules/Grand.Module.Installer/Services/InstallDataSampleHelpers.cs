using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Media;
using Grand.Domain.Seo;
using Grand.Module.Installer.Extensions;

namespace Grand.Module.Installer.Services;

public partial class InstallationService
{
    protected virtual async Task<Picture> InsertSamplePicture(string fileName, string seoName, Reference reference, string objectId, string? alt = null)
    {
        var mime = Path.GetExtension(fileName).ToLowerInvariant() == ".png" ? "image/png" : "image/jpeg";
        var bytes = await File.ReadAllBytesAsync(Path.Combine(GetSamplesPath(), fileName));
        return await _pictureRepository.InsertPicture(bytes, mime, SeoExtensions.GenerateSlug(seoName, false, false, false),
            alt ?? seoName, alt ?? seoName, false, reference, objectId);
    }

    protected async Task AddProductPictures(Product product, params string[] fileNames)
    {
        for (var i = 0; i < fileNames.Length; i++)
        {
            var picture = await InsertSamplePicture(fileNames[i], product.Name, Reference.Product, product.Id, product.Name);
            product.ProductPictures.Add(new ProductPicture { PictureId = picture.Id, DisplayOrder = i, IsDefault = i == 0 });
        }
    }

    protected async Task InsertSlug(string entityId, string entityName, string name, Action<string> setSeName)
    {
        var seName = SeoExtensions.GenerateSlug(name, false, false, false);
        await _entityUrlRepository.InsertAsync(new EntityUrl {
            EntityId = entityId, EntityName = entityName, LanguageId = "", IsActive = true, Slug = seName
        });
        setSeName(seName);
    }

    protected string CategoryId(string name) => _categoryRepository.Table.Single(x => x.Name == name).Id;
    protected string BrandId(string name) => _brandRepository.Table.Single(x => x.Name == name).Id;
    protected string VendorId(string name) => _vendorRepository.Table.Single(x => x.Name == name).Id;
    protected string TaxCategoryId(string name) => _taxCategoryRepository.Table.Single(x => x.Name == name).Id;
    protected string ProductAttributeId(string name) => _productAttributeRepository.Table.Single(x => x.Name == name).Id;
    protected Product ProductByName(string name) => _productRepository.Table.Single(x => x.Name == name);

    protected ProductSpecificationAttribute Spec(string attributeName, string optionName, int displayOrder = 0)
    {
        var attribute = _specificationAttributeRepository.Table.Single(x => x.Name == attributeName);
        return new ProductSpecificationAttribute {
            SpecificationAttributeId = attribute.Id,
            SpecificationAttributeOptionId = attribute.SpecificationAttributeOptions.Single(o => o.Name == optionName).Id,
            AllowFiltering = true, ShowOnProductPage = true, DisplayOrder = displayOrder
        };
    }

    protected sealed record SampleProductContext(
        string SimpleLayoutId, string GroupedLayoutId, string DeliveryDateId, string StoreId, string CustomerId);
}
