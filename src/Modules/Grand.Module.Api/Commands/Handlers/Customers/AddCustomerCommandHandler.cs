using Grand.Module.Api.Commands.Models.Customers;
using Grand.Module.Api.DTOs.Customers;
using Grand.Module.Api.Extensions;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Customers;
using MediatR;

namespace Grand.Module.Api.Commands.Handlers.Customers;

public class AddCustomerCommandHandler : IRequestHandler<AddCustomerCommand, CustomerDto>
{
    private readonly ICustomerService _customerService;
    private readonly IGroupService _groupService;

    public AddCustomerCommandHandler(
        ICustomerService customerService,
        IGroupService groupService)
    {
        _customerService = customerService;
        _groupService = groupService;
    }

    public async Task<CustomerDto> Handle(AddCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = request.Model.ToEntity();
        customer.LastActivityDateUtc = DateTime.UtcNow;
        if (string.IsNullOrEmpty(customer.Username))
            customer.Username = customer.Email;

        await _customerService.InsertCustomer(customer);
        await SaveCustomerAttributes(request.Model, customer);
        await SaveCustomerGroups(request.Model, customer);

        return customer.ToModel();
    }

    private async Task SaveCustomerAttributes(CustomerDto model, Customer customer)
    {
        await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.VatNumber, model.VatNumber);
        await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.VatNumberStatusId, model.VatNumberStatusId);
        await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.Gender, model.Gender);
        await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.FirstName, model.FirstName);
        await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.LastName, model.LastName);
        await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.DateOfBirth, model.DateOfBirth);
        await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.Company, model.Company);
        await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.StreetAddress, model.StreetAddress);
        await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.StreetAddress2, model.StreetAddress2);
        await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.ZipPostalCode, model.ZipPostalCode);
        await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.City, model.City);
        await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.CountryId, model.CountryId);
        await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.StateProvinceId, model.StateProvinceId);
        await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.Phone, model.Phone);
        await _customerService.UpdateUserField(customer, SystemCustomerFieldNames.Fax, model.Fax);
    }

    protected async Task SaveCustomerGroups(CustomerDto model, Customer customer)
    {
        var insertgroup = model.Groups.Except(customer.Groups.Select(x => x)).ToList();
        foreach (var item in insertgroup)
        {
            var group = await _groupService.GetCustomerGroupById(item);
            if (group != null)
            {
                customer.Groups.Add(item);
                await _customerService.InsertCustomerGroupInCustomer(group, customer.Id);
            }
        }

        var deletegroup = customer.Groups.Select(x => x).Except(model.Groups).ToList();
        foreach (var item in deletegroup)
        {
            var group = await _groupService.GetCustomerGroupById(item);
            if (group != null)
            {
                customer.Groups.Remove(customer.Groups.FirstOrDefault(x => x == item));
                await _customerService.DeleteCustomerGroupInCustomer(group, customer.Id);
            }
        }
    }
}