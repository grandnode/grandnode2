using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Models.Catalog;

namespace Grand.Web.Admin.Validators.Catalog;

public class CollectionProductModelValidator : BaseStoreAccessValidator<CollectionModel.CollectionProductModel, Collection>
{
    private readonly ICollectionService _collectionService;
    public CollectionProductModelValidator(
        IEnumerable<IValidatorConsumer<CollectionModel.CollectionProductModel>> validators,
        ITranslationService translationService, ICollectionService collectionService, IContextAccessor contextAccessor)
        : base(validators, translationService, contextAccessor)
    {
        _collectionService = collectionService;
    }
    protected override async Task<Collection> GetEntity(CollectionModel.CollectionProductModel model)
    {
        return await _collectionService.GetCollectionById(model.CollectionId);
    }

    protected override string GetPermissionsResourceKey => "Admin.Catalog.Collections.Permissions";
}