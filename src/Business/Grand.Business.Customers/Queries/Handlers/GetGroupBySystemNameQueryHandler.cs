using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Queries.Customers;
using Grand.Domain.Customers;
using MediatR;

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
