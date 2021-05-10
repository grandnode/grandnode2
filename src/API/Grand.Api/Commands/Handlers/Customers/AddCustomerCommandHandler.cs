using Grand.Api.Commands.Models.Customers;
using Grand.Api.DTOs.Customers;
using Grand.Api.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Customers.Interfaces;
using Grand.Domain.Customers;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Api.Commands.Handlers.Customers
{
    public class AddCustomerCommandHandler : IRequestHandler<AddCustomerCommand, CustomerDto>
    {
        private readonly ICustomerService _customerService;
        private readonly IGroupService _groupService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly ITranslationService _translationService;
        private readonly IUserFieldService _userFieldsService;

        public AddCustomerCommandHandler(
            ICustomerService customerService,
            IGroupService groupService,
            ICustomerActivityService customerActivityService,
            ITranslationService translationService,
            IUserFieldService userFieldsService)
        {
            _customerService = customerService;
            _groupService = groupService;
            _customerActivityService = customerActivityService;
            _translationService = translationService;
            _userFieldsService = userFieldsService;
        }

        public async Task<CustomerDto> Handle(AddCustomerCommand request, CancellationToken cancellationToken)
        {
            var customer = request.Model.ToEntity();
            customer.CreatedOnUtc = DateTime.UtcNow;
            customer.LastActivityDateUtc = DateTime.UtcNow;
            if (string.IsNullOrEmpty(customer.Username))
                customer.Username = customer.Email;

            await _customerService.InsertCustomer(customer);
            await SaveCustomerAttributes(request.Model, customer);
            await SaveCustomerGroups(request.Model, customer);

            //activity log
            await _customerActivityService.InsertActivity("AddNewCustomer", customer.Id, _translationService.GetResource("ActivityLog.AddNewCustomer"), customer.Id);
            return customer.ToModel();
        }

        protected async Task SaveCustomerAttributes(CustomerDto model, Customer customer)
        {
            await _userFieldsService.SaveField(customer, SystemCustomerFieldNames.VatNumber, model.VatNumber);
            await _userFieldsService.SaveField(customer, SystemCustomerFieldNames.VatNumberStatusId, model.VatNumberStatusId);
            await _userFieldsService.SaveField(customer, SystemCustomerFieldNames.Gender, model.Gender);
            await _userFieldsService.SaveField(customer, SystemCustomerFieldNames.FirstName, model.FirstName);
            await _userFieldsService.SaveField(customer, SystemCustomerFieldNames.LastName, model.LastName);
            await _userFieldsService.SaveField(customer, SystemCustomerFieldNames.DateOfBirth, model.DateOfBirth);
            await _userFieldsService.SaveField(customer, SystemCustomerFieldNames.Company, model.Company);
            await _userFieldsService.SaveField(customer, SystemCustomerFieldNames.StreetAddress, model.StreetAddress);
            await _userFieldsService.SaveField(customer, SystemCustomerFieldNames.StreetAddress2, model.StreetAddress2);
            await _userFieldsService.SaveField(customer, SystemCustomerFieldNames.ZipPostalCode, model.ZipPostalCode);
            await _userFieldsService.SaveField(customer, SystemCustomerFieldNames.City, model.City);
            await _userFieldsService.SaveField(customer, SystemCustomerFieldNames.CountryId, model.CountryId);
            await _userFieldsService.SaveField(customer, SystemCustomerFieldNames.StateProvinceId, model.StateProvinceId);
            await _userFieldsService.SaveField(customer, SystemCustomerFieldNames.Phone, model.Phone);
            await _userFieldsService.SaveField(customer, SystemCustomerFieldNames.Fax, model.Fax);
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
}
