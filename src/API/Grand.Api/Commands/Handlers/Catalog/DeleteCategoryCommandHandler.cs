using Grand.Business.Catalog.Interfaces.Categories;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Commands.Models.Catalog
{
    public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, bool>
    {
        private readonly ICategoryService _categoryService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ITranslationService _translationService;

        public DeleteCategoryCommandHandler(
            ICategoryService categoryService,
            ICustomerActivityService customerActivityService,
            ITranslationService translationService)
        {
            _categoryService = categoryService;
            _customerActivityService = customerActivityService;
            _translationService = translationService;
        }

        public async Task<bool> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await _categoryService.GetCategoryById(request.Model.Id);
            if (category != null)
            {
                await _categoryService.DeleteCategory(category);

                //activity log
                await _customerActivityService.InsertActivity("DeleteCategory", category.Id, _translationService.GetResource("ActivityLog.DeleteCategory"), category.Name);
            }
            return true;
        }
    }
}
