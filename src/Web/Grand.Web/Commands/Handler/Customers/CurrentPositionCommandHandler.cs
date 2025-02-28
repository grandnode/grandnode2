using Grand.Business.Core.Interfaces.Marketing.Customers;
using Grand.Web.Commands.Models.Customers;
using MediatR;

namespace Grand.Web.Commands.Handler.Customers;

public class CurrentPositionCommandHandler : IRequestHandler<CurrentPositionCommand, bool>
{
    private readonly ICustomerCoordinatesService _customerCoordinateService;

    public CurrentPositionCommandHandler(ICustomerCoordinatesService customerCoordinateService)
    {
        _customerCoordinateService = customerCoordinateService;
    }

    public async Task<bool> Handle(CurrentPositionCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Customer);
        ArgumentNullException.ThrowIfNull(request.Model);

        await _customerCoordinateService.SaveGeoCoordinate(request.Customer, request.Model.Longitude,
            request.Model.Latitude);

        return true;
    }
}