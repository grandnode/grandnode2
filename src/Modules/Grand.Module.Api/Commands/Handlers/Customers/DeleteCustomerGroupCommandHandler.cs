using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Module.Api.Commands.Models.Customers;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Customers;

public class DeleteCustomerGroupCommandHandler : IRequestHandler<DeleteCustomerGroupCommand, bool>
{
    private readonly IGroupService _groupService;

    public DeleteCustomerGroupCommandHandler(IGroupService groupService)
    {
        _groupService = groupService;
    }

    public async Task<bool> Handle(DeleteCustomerGroupCommand request, CancellationToken cancellationToken)
    {
        var customerGroup = await _groupService.GetCustomerGroupById(request.Model.Id);
        if (customerGroup != null) await _groupService.DeleteCustomerGroup(customerGroup);
        return true;
    }
}