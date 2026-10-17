using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Domain.Catalog;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.Common.Localization;

namespace Grand.Web.AdminShared.Validators.Catalog;

public class CollectionProductModelValidator : BaseStoreAccessValidator<CollectionModel.CollectionProductModel, Collection>
{
    private readonly ICollectionService _collectionService;
    public CollectionProductModelValidator(
        IEnumerable<IValidatorConsumer<CollectionModel.CollectionProductModel>> validators,
        IRequestTranslationService translationService, ICollectionService collectionService, IContextAccessor contextAccessor)
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