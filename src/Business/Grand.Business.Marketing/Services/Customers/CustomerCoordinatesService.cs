using Grand.Business.Core.Events.Marketing;
using Grand.Business.Core.Interfaces.Marketing.Customers;
using Grand.Data;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Mediator;

namespace Grand.Business.Marketing.Services.Customers;

public class CustomerCoordinatesService : ICustomerCoordinatesService
{
    private readonly IRepository<Customer> _customerRepository;
    private readonly IMediator _mediator;

    public CustomerCoordinatesService(
        IRepository<Customer> customerRepository,
        IMediator mediator)
    {
        _customerRepository = customerRepository;
        _mediator = mediator;
    }

    public Task<(double longitude, double latitude)> GetGeoCoordinate(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        return customer.Coordinates == null
            ? Task.FromResult((0d, 0d))
            : Task.FromResult((customer.Coordinates.X, customer.Coordinates.Y));
    }

    public async Task SaveGeoCoordinate(Customer customer, double longitude, double latitude)
    {
        customer.Coordinates = new GeoCoordinates(longitude, latitude);

        //update customer
        await _customerRepository.UpdateField(customer.Id, x => x.Coordinates, customer.Coordinates);

        //raise event       
        await _mediator.Publish(new CustomerCoordinatesEvent(customer));
    }
}