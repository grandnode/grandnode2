using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Customers.Queries.Models;
using Grand.Domain.Customers;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Customers.Queries.Handlers
{
    public class GetGroupBySystemNameQueryHandler : IRequestHandler<GetGroupBySystemNameQuery, CustomerGroup>
    {
        private readonly IGroupService _groupService;

        public GetGroupBySystemNameQueryHandler(IGroupService groupService)
        {
            _groupService = groupService;
        }

        public async Task<CustomerGroup> Handle(GetGroupBySystemNameQuery request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(request.SystemName))
                throw new ArgumentNullException("Request.SystemName");

            return await _groupService.GetCustomerGroupBySystemName(request.SystemName);
        }
    }
}
