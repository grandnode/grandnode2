using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Module.Api.Commands.Models.Customers;
using Grand.Module.Api.DTOs.Customers;
using Grand.Module.Api.Extensions;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Customers;

public class UpdateCustomerGroupCommandHandler : IRequestHandler<UpdateCustomerGroupCommand, CustomerGroupDto>
{
    private readonly IGroupService _groupService;

    public UpdateCustomerGroupCommandHandler(IGroupService groupService)
    {
        _groupService = groupService;
    }

    public async Task<CustomerGroupDto> Handle(UpdateCustomerGroupCommand request, CancellationToken cancellationToken)
    {
        var customerGroup = await _groupService.GetCustomerGroupById(request.Model.Id);
        customerGroup = request.Model.ToEntity(customerGroup);
        await _groupService.UpdateCustomerGroup(customerGroup);

        return customerGroup.ToModel();
    }
}