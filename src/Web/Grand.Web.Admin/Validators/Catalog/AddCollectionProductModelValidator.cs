using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Models.Catalog;

namespace Grand.Web.Admin.Validators.Catalog;

public class AddCollectionProductModelValidator : BaseStoreAccessValidator<CollectionModel.AddCollectionProductModel, Collection>
{
    private readonly ICollectionService _collectionService;
    public AddCollectionProductModelValidator(
        IEnumerable<IValidatorConsumer<CollectionModel.AddCollectionProductModel>> validators,
        ITranslationService translationService, ICollectionService collectionService, IContextAccessor contextAccessor)
        : base(validators, translationService, contextAccessor)
    {
        _collectionService = collectionService;
    }
    protected override async Task<Collection> GetEntity(CollectionModel.AddCollectionProductModel model)
    {
        return await _collectionService.GetCollectionById(model.CollectionId);
    }
    protected override string GetPermissionsResourceKey => "Admin.Catalog.Products.Permissions";
}