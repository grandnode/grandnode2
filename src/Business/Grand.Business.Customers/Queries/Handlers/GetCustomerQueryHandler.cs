using Grand.Business.Customers.Queries.Models;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.Customers.Queries.Handlers
{
    public class GetCustomerQueryHandler : IRequestHandler<GetCustomerQuery, IQueryable<Customer>>
    {
        private readonly IRepository<Customer> _customerRepository;

        public GetCustomerQueryHandler(IRepository<Customer> customerRepository)
        {
            _customerRepository = customerRepository;
        }
        public Task<IQueryable<Customer>> Handle(GetCustomerQuery request, CancellationToken cancellationToken)
        {
            var query = from p in _customerRepository.Table
                        select p;

            if (request.CreatedFromUtc.HasValue)
                query = query.Where(c => request.CreatedFromUtc.Value <= c.CreatedOnUtc);
            if (request.CreatedToUtc.HasValue)
                query = query.Where(c => request.CreatedToUtc.Value >= c.CreatedOnUtc);
            if (!string.IsNullOrEmpty(request.AffiliateId))
                query = query.Where(c => request.AffiliateId == c.AffiliateId);
            if (!string.IsNullOrEmpty(request.VendorId))
                query = query.Where(c => request.VendorId == c.VendorId);
            if (!string.IsNullOrEmpty(request.StoreId))
                query = query.Where(c => c.StoreId == request.StoreId);
            if (!string.IsNullOrEmpty(request.OwnerId))
                query = query.Where(c => c.OwnerId == request.OwnerId);
            if (!string.IsNullOrEmpty(request.SalesEmployeeId))
                query = query.Where(c => c.SeId == request.SalesEmployeeId);

            query = query.Where(c => !c.Deleted);
            if (request.CustomerGroupIds != null && request.CustomerGroupIds.Length > 0)
                query = query.Where(c => c.Groups.Any(x => request.CustomerGroupIds.Contains(x)));
            if (request.CustomerTagIds != null && request.CustomerTagIds.Length > 0)
            {
                foreach (var item in request.CustomerTagIds)
                {
                    query = query.Where(c => c.CustomerTags.Contains(item));
                }
            }
            if (!string.IsNullOrWhiteSpace(request.Email))
                query = query.Where(c => c.Email != null && c.Email.Contains(request.Email.ToLower()));
            if (!string.IsNullOrWhiteSpace(request.Username))
                query = query.Where(c => c.Username != null && c.Username.ToLower().Contains(request.Username.ToLower()));

            if (!string.IsNullOrWhiteSpace(request.FirstName))
            {
                query = query.Where(x => x.UserFields.Any(y => y.Key == SystemCustomerFieldNames.FirstName && y.Value != null && y.Value.ToLower().Contains(request.FirstName.ToLower())));
            }

            if (!string.IsNullOrWhiteSpace(request.LastName))
            {
                query = query.Where(x => x.UserFields.Any(y => y.Key == SystemCustomerFieldNames.LastName && y.Value != null && y.Value.ToLower().Contains(request.LastName.ToLower())));
            }

            //search by company
            if (!string.IsNullOrWhiteSpace(request.Company))
            {
                query = query.Where(x => x.UserFields.Any(y => y.Key == SystemCustomerFieldNames.Company && y.Value != null && y.Value.ToLower().Contains(request.Company.ToLower())));
            }
            //search by phone
            if (!string.IsNullOrWhiteSpace(request.Phone))
            {
                query = query.Where(x => x.UserFields.Any(y => y.Key == SystemCustomerFieldNames.Phone && y.Value != null && y.Value.ToLower().Contains(request.Phone.ToLower())));
            }
            //search by zip
            if (!string.IsNullOrWhiteSpace(request.ZipPostalCode))
            {
                query = query.Where(x => x.UserFields.Any(y => y.Key == SystemCustomerFieldNames.ZipPostalCode && y.Value != null && y.Value.ToLower().Contains(request.ZipPostalCode.ToLower())));
            }

            if (request.LoadOnlyWithShoppingCart)
            {
                query = request.Sct.HasValue ?
                    query.Where(c => c.ShoppingCartItems.Any(x => x.ShoppingCartTypeId == request.Sct.Value)) :
                    query.Where(c => c.ShoppingCartItems.Count() > 0);
            }

            if (request.OrderBySelector == null)
                query = query.OrderByDescending(c => c.CreatedOnUtc);
            else
                query = query.OrderByDescending(request.OrderBySelector);

            return Task.FromResult(query);
        }
    }
}
