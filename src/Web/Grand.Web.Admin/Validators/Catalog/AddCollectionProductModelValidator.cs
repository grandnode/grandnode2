using FluentValidation;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Catalog;

namespace Grand.Web.Admin.Validators.Catalog
{
    public class AddCollectionProductModelValidator : BaseGrandValidator<CollectionModel.AddCollectionProductModel>
    {
        public AddCollectionProductModelValidator(
            IEnumerable<IValidatorConsumer<CollectionModel.AddCollectionProductModel>> validators,
            ITranslationService translationService, ICollectionService collectionService, IWorkContext workContext)
            : base(validators)
        {
            if (!string.IsNullOrEmpty(workContext.CurrentCustomer.StaffStoreId))
            {
                RuleFor(x => x).MustAsync(async (x, y, context) =>
                {
                    var collection = await collectionService.GetCollectionById(x.CollectionId);
                    if (collection != null)
                        if (!collection.AccessToEntityByStore(workContext.CurrentCustomer.StaffStoreId))
                            return false;

                    return true;
                }).WithMessage(translationService.GetResource("Admin.Catalog.Products.Permisions"));
            }
        }
    }
}