using Grand.Business.Catalog.Interfaces.Categories;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Infrastructure;
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
        private readonly IWorkContext _workContext;

        public DeleteCategoryCommandHandler(
            ICategoryService categoryService,
            ICustomerActivityService customerActivityService,
            ITranslationService translationService,
            IWorkContext workContext)
        {
            _categoryService = categoryService;
            _customerActivityService = customerActivityService;
            _translationService = translationService;
            _workContext = workContext;
        }

        public async Task<bool> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
        {
            var category = await _categoryService.GetCategoryById(request.Model.Id);
            if (category != null)
            {
                await _categoryService.DeleteCategory(category);

                //activity log
                _ = _customerActivityService.InsertActivity("DeleteCategory", category.Id, _workContext.CurrentCustomer, "", _translationService.GetResource("ActivityLog.DeleteCategory"), category.Name);
            }
            return true;
        }
    }
}
