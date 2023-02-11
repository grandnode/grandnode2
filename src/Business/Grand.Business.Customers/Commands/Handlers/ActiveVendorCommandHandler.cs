﻿using Grand.Business.Core.Commands.Customers;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Events.Customers;
using Grand.Domain.Customers;
using MediatR;
using Grand.Business.Core.Interfaces.Common.Directory;

namespace Grand.Business.Customers.Commands.Handlers
{
    public class ActiveVendorCommandHandler : IRequestHandler<ActiveVendorCommand, bool>
    {
        private readonly IVendorService _vendorService;
        private readonly ICustomerService _customerService;
        private readonly IGroupService _groupService; 
        private readonly IMediator _mediator;

        public ActiveVendorCommandHandler(
            IVendorService vendorService,
            ICustomerService customerService,
            IGroupService groupService,
            IMediator mediator)
        {
            _vendorService = vendorService;
            _customerService = customerService;
            _groupService = groupService;
            _mediator = mediator;
        }

        public async Task<bool> Handle(ActiveVendorCommand request, CancellationToken cancellationToken)
        {
            //update vendor - set active
            request.Vendor.Active = request.Active;
            await _vendorService.UpdateVendor(request.Vendor);

            //assign vendor group for customers
            var vendorGroup = await _groupService.GetCustomerGroupBySystemName(SystemCustomerGroupNames.Vendors);

            foreach (var item in request.CustomerIds)
            {
                var customer = await _customerService.GetCustomerById(item);
                if (customer is not { Deleted: false, Active: true } || customer.IsSystemAccount() ||
                    await _groupService.IsAdmin(customer)) continue;
                if (vendorGroup == null) continue;
                if (request.Active)
                {
                    if (!await _groupService.IsVendor(customer))
                        await _customerService.InsertCustomerGroupInCustomer(vendorGroup, item);
                }
                else
                {
                    if (await _groupService.IsVendor(customer))
                        await _customerService.DeleteCustomerGroupInCustomer(vendorGroup, item);
                }
            }

            //raise event       
            await _mediator.Publish(new VendorActivationEvent(request.Vendor), cancellationToken);

            return true;
        }
    }
}
