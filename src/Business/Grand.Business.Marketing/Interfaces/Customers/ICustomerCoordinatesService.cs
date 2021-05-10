using Grand.Domain.Customers;
using System.Threading.Tasks;

namespace Grand.Business.Marketing.Interfaces.Customers
{
    public interface ICustomerCoordinatesService
    {
        Task<(double longitude, double latitude)> GetGeoCoordinate();
        Task<(double longitude, double latitude)> GetGeoCoordinate(Customer customer);
        Task SaveGeoCoordinate(double longitude, double latitude);
        Task SaveGeoCoordinate(Customer customer, double longitude, double latitude);
    }
}
