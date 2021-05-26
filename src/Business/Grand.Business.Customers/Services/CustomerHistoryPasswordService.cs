using Grand.Business.Customers.Interfaces;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Infrastructure.Extensions;
using MediatR;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Grand.Business.Customers.Services
{
    public class CustomerHistoryPasswordService : ICustomerHistoryPasswordService
    {
        private readonly IRepository<CustomerHistoryPassword> _customerHistoryPasswordProductRepository;
        private readonly IMediator _mediator;

        public CustomerHistoryPasswordService(IRepository<CustomerHistoryPassword> customerHistoryPasswordProductRepository,
            IMediator mediator)
        {
            _customerHistoryPasswordProductRepository = customerHistoryPasswordProductRepository;
            _mediator = mediator;
        }

        /// <summary>
        /// Insert a customer history password
        /// </summary>
        /// <param name="customer">Customer</param>
        public virtual async Task InsertCustomerPassword(Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            var chp = new CustomerHistoryPassword
            {
                Password = customer.Password,
                PasswordFormatId = customer.PasswordFormatId,
                PasswordSalt = customer.PasswordSalt,
                CustomerId = customer.Id,
                CreatedOnUtc = DateTime.UtcNow
            };

            await _customerHistoryPasswordProductRepository.InsertAsync(chp);

            //event notification
            await _mediator.EntityInserted(chp);
        }

        /// <summary>
        /// Gets customer passwords
        /// </summary>
        /// <param name="customerId">Customer identifier; pass null to load all records</param>
        /// <param name="passwordsToReturn">Number of returning passwords; pass null to load all records</param>
        /// <returns>List of customer passwords</returns>
        public virtual async Task<IList<CustomerHistoryPassword>> GetPasswords(string customerId, int passwordsToReturn)
        {
            return await Task.FromResult(_customerHistoryPasswordProductRepository
                    .Table
                    .Where(x=>x.CustomerId == customerId)
                    .OrderByDescending(password => password.CreatedOnUtc)
                    .Take(passwordsToReturn)
                    .ToList());
        }

    }
}
