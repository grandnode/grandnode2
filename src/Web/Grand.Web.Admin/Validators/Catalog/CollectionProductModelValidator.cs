﻿using FluentValidation;
using Grand.Business.Core.Interfaces.Catalog.Collections;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using Grand.Infrastructure.Validators;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Models.Catalog;

namespace Grand.Web.Admin.Validators.Catalog;

public class CollectionProductModelValidator : BaseGrandValidator<CollectionModel.CollectionProductModel>
{
    public CollectionProductModelValidator(
        IEnumerable<IValidatorConsumer<CollectionModel.CollectionProductModel>> validators,
        ITranslationService translationService, ICollectionService collectionService, IWorkContextAccessor workContextAccessor)
        : base(validators)
    {
        if (!string.IsNullOrEmpty(workContextAccessor.WorkContext.CurrentCustomer.StaffStoreId))
            RuleFor(x => x).MustAsync(async (x, _, _) =>
            {
                var collection = await collectionService.GetCollectionById(x.CollectionId);
                if (collection != null)
                    if (!collection.AccessToEntityByStore(workContextAccessor.WorkContext.CurrentCustomer.StaffStoreId))
                        return false;

                return true;
            }).WithMessage(translationService.GetResource("Admin.Catalog.Collections.Permissions"));
    }
}