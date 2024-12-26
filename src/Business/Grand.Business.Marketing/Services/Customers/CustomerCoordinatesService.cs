﻿using Grand.Business.Core.Events.Marketing;
using Grand.Business.Core.Interfaces.Marketing.Customers;
using Grand.Data;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Infrastructure;
using MediatR;

namespace Grand.Business.Marketing.Services.Customers;

public class CustomerCoordinatesService : ICustomerCoordinatesService
{
    private readonly IRepository<Customer> _customerRepository;
    private readonly IMediator _mediator;
    private readonly IWorkContextAccessor _workContextAccessor;

    public CustomerCoordinatesService(
        IRepository<Customer> customerRepository,
        IWorkContextAccessor workContextAccessor,
        IMediator mediator)
    {
        _customerRepository = customerRepository;
        _workContextAccessor = workContextAccessor;
        _mediator = mediator;
    }

    public Task<(double longitude, double latitude)> GetGeoCoordinate()
    {
        return GetGeoCoordinate(_workContextAccessor.WorkContext.CurrentCustomer);
    }

    public async Task<(double longitude, double latitude)> GetGeoCoordinate(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        if (customer.Coordinates == null)
            await Task.FromResult((0, 0));

        return await Task.FromResult((customer.Coordinates!.X, customer.Coordinates!.Y));
    }

    public async Task SaveGeoCoordinate(double longitude, double latitude)
    {
        await SaveGeoCoordinate(_workContextAccessor.WorkContext.CurrentCustomer, longitude, latitude);
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