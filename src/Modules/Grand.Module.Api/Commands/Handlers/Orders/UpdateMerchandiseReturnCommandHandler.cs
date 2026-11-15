using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Mediator;
using Grand.Module.Api.Commands.Models.Orders;
using Grand.Module.Api.Extensions;

namespace Grand.Module.Api.Commands.Handlers.Orders;

public class UpdateMerchandiseReturnCommandHandler : IRequestHandler<UpdateMerchandiseReturnCommand, string>
{
    private readonly IMerchandiseReturnWorkflowService _merchandiseReturnWorkflowService;

    public UpdateMerchandiseReturnCommandHandler(IMerchandiseReturnWorkflowService merchandiseReturnWorkflowService)
    {
        _merchandiseReturnWorkflowService = merchandiseReturnWorkflowService;
    }

    public Task<string> Handle(UpdateMerchandiseReturnCommand request, CancellationToken cancellationToken)
    {
        return _merchandiseReturnWorkflowService.Update(request.Id, stored =>
        {
            stored.CustomerComments = request.Model.CustomerComments;
            stored.StaffNotes = request.Model.StaffNotes;
            stored.ExternalId = request.Model.ExternalId;
            stored.PickupDate = request.Model.PickupDate;
            stored.PickupAddress = request.Model.PickupAddress?.ToEntity();
        });
    }
}
