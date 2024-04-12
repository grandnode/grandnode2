using Grand.Api.Commands.Models.Customers;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using MediatR;

namespace Grand.Api.Commands.Handlers.Customers;

public class DeleteCustomerGroupCommandHandler : IRequestHandler<DeleteCustomerGroupCommand, bool>
{
    private readonly IGroupService _groupService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContext _workContext;

    public DeleteCustomerGroupCommandHandler(
        IGroupService groupService,
        ITranslationService translationService,
        IWorkContext workContext)
    {
        _groupService = groupService;
        _translationService = translationService;
        _workContext = workContext;
    }

    public async Task<bool> Handle(DeleteCustomerGroupCommand request, CancellationToken cancellationToken)
    {
        var customerGroup = await _groupService.GetCustomerGroupById(request.Model.Id);
        if (customerGroup != null) await _groupService.DeleteCustomerGroup(customerGroup);
        return true;
    }
}