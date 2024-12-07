﻿using Grand.Module.Api.Commands.Models.Customers;
using Grand.Module.Api.DTOs.Customers;
using Grand.Module.Api.Extensions;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Infrastructure;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Customers;

public class AddCustomerGroupCommandHandler : IRequestHandler<AddCustomerGroupCommand, CustomerGroupDto>
{
    private readonly IGroupService _groupService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContext _workContext;

    public AddCustomerGroupCommandHandler(
        IGroupService groupService,
        ITranslationService translationService,
        IWorkContext workContext)
    {
        _groupService = groupService;
        _translationService = translationService;
        _workContext = workContext;
    }

    public async Task<CustomerGroupDto> Handle(AddCustomerGroupCommand request, CancellationToken cancellationToken)
    {
        var customergroup = request.Model.ToEntity();
        await _groupService.InsertCustomerGroup(customergroup);

        return customergroup.ToModel();
    }
}