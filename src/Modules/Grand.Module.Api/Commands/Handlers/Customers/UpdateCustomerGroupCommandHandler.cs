using Grand.Module.Api.Commands.Models.Customers;
using Grand.Module.Api.DTOs.Customers;
using Grand.Module.Api.Extensions;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Customers;

public class UpdateCustomerGroupCommandHandler : IRequestHandler<UpdateCustomerGroupCommand, CustomerGroupDto>
{
    private readonly IGroupService _groupService;
    private readonly ITranslationService _translationService;
    private readonly IContextAccessor _contextAccessor;

    public UpdateCustomerGroupCommandHandler(
        IGroupService groupService,
        ITranslationService translationService,
        IContextAccessor contextAccessor)
    {
        _groupService = groupService;
        _translationService = translationService;
        _contextAccessor = contextAccessor;
    }

    public async Task<CustomerGroupDto> Handle(UpdateCustomerGroupCommand request, CancellationToken cancellationToken)
    {
        var customerGroup = await _groupService.GetCustomerGroupById(request.Model.Id);
        customerGroup = request.Model.ToEntity(customerGroup);
        await _groupService.UpdateCustomerGroup(customerGroup);

        return customerGroup.ToModel();
    }
}