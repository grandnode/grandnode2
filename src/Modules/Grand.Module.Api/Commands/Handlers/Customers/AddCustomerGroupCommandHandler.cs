using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Module.Api.Commands.Models.Customers;
using Grand.Module.Api.DTOs.Customers;
using Grand.Module.Api.Extensions;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Customers;

public class AddCustomerGroupCommandHandler : IRequestHandler<AddCustomerGroupCommand, CustomerGroupDto>
{
    private readonly IGroupService _groupService;

    public AddCustomerGroupCommandHandler(IGroupService groupService)
    {
        _groupService = groupService;
    }

    public async Task<CustomerGroupDto> Handle(AddCustomerGroupCommand request, CancellationToken cancellationToken)
    {
        var customergroup = request.Model.ToEntity();
        await _groupService.InsertCustomerGroup(customergroup);

        return customergroup.ToModel();
    }
}