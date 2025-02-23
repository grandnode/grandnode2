using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure.Validators;
using Grand.Infrastructure;
using FluentValidation;
using Grand.Domain;
using Grand.Domain.Stores;
using Grand.Web.Admin.Extensions;

namespace Grand.Web.Admin.Validators.Catalog
{
    public abstract class BaseStoreAccessValidator<TModel, TEntity> : BaseGrandValidator<TModel> where TEntity : BaseEntity, IStoreLinkEntity where TModel : class
    {
        protected readonly IContextAccessor _contextAccessor;
        protected readonly ITranslationService _translationService;

        protected BaseStoreAccessValidator(
            IEnumerable<IValidatorConsumer<TModel>> validators,
            ITranslationService translationService,
            IContextAccessor contextAccessor)
            : base(validators)
        {
            _contextAccessor = contextAccessor;
            _translationService = translationService;

            if (!string.IsNullOrEmpty(_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId))
            {
                RuleFor(x => x).MustAsync(async (model, cancellationToken) =>
                {
                    var entity = await GetEntity(model);
                    if (entity != null && !entity.AccessToEntityByStore(_contextAccessor.WorkContext.CurrentCustomer.StaffStoreId))
                        return false;
                    return true;
                }).WithMessage(_translationService.GetResource(GetPermissionsResourceKey));
            }
        }
        protected abstract Task<TEntity> GetEntity(TModel model);
        protected abstract string GetPermissionsResourceKey { get; }
    }
}
