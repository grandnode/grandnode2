using Grand.Domain.Customers;

namespace Grand.Business.Core.Interfaces.Marketing.Customers;

public interface ICustomerCoordinatesService
{
    Task<(double longitude, double latitude)> GetGeoCoordinate(Customer customer);
    Task SaveGeoCoordinate(Customer customer, double longitude, double latitude);
}