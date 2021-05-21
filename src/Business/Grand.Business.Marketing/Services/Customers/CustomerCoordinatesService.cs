using Grand.Business.Marketing.Interfaces.Customers;
using Grand.Business.Marketing.Events;
using Grand.Infrastructure;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using MediatR;
using System;
using System.Threading.Tasks;

namespace Grand.Business.Marketing.Services.Customers
{
    public class CustomerCoordinatesService : ICustomerCoordinatesService
    {
        private readonly IRepository<Customer> _customerRepository;
        private readonly IWorkContext _workContext;
        private readonly IMediator _mediator;

        public CustomerCoordinatesService(
            IRepository<Customer> customerRepository,
            IWorkContext workContext,
            IMediator mediator)
        {
            _customerRepository = customerRepository;
            _workContext = workContext;
            _mediator = mediator;
        }

        public Task<(double longitude, double latitude)> GetGeoCoordinate()
        {
            return GetGeoCoordinate(_workContext.CurrentCustomer);
        }

        public async Task<(double longitude, double latitude)> GetGeoCoordinate(Customer customer)
        {
            if (customer == null)
                throw new ArgumentNullException(nameof(customer));

            if (customer.Coordinates == null)
                await Task.FromResult((0, 0));

            return await Task.FromResult((customer.Coordinates.X, customer.Coordinates.Y));
        }

        public async Task SaveGeoCoordinate(double longitude, double latitude)
        {
            await SaveGeoCoordinate(_workContext.CurrentCustomer, longitude, latitude);
        }

        public async Task SaveGeoCoordinate(Customer customer, double longitude, double latitude)
        {
            customer.Coordinates = new Domain.Common.GeoCoordinates(longitude, latitude);

            //update customer
            await _customerRepository.UpdateField(customer.Id, x => x.Coordinates, customer.Coordinates);

            //raise event       
            await _mediator.Publish(new CustomerCoordinatesEvent(customer));
        }
    }
}
