using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Domain.Orders;
using Grand.Mediator;
using Grand.Module.Api.Commands.Models.Orders;
using Grand.Module.Api.Extensions;

namespace Grand.Module.Api.Commands.Handlers.Orders;

public class UpdateMerchandiseReturnCommandHandler : IRequestHandler<UpdateMerchandiseReturnCommand, string>
{
    private readonly IMerchandiseReturnWorkflowService _merchandiseReturnWorkflowService;
    private readonly OrderSettings _orderSettings;

    public UpdateMerchandiseReturnCommandHandler(IMerchandiseReturnWorkflowService merchandiseReturnWorkflowService,
        OrderSettings orderSettings)
    {
        _merchandiseReturnWorkflowService = merchandiseReturnWorkflowService;
        _orderSettings = orderSettings;
    }

    public Task<string> Handle(UpdateMerchandiseReturnCommand request, CancellationToken cancellationToken)
    {
        //a field the client did not send stays as stored: a PUT of the staff notes alone keeps the customer's comment
        //and the pickup address; an empty string clears a text
        var model = request.Model;
        return _merchandiseReturnWorkflowService.Update(request.Id, stored =>
        {
            if (model.CustomerComments != null)
                stored.CustomerComments = model.CustomerComments;
            if (model.StaffNotes != null)
                stored.StaffNotes = model.StaffNotes;
            if (model.ExternalId != null)
                stored.ExternalId = model.ExternalId;
            //the pickup date and address change only where the store asks for them, as in the panel
            if (model.PickupDate.HasValue && _orderSettings.MerchandiseReturns_AllowToSpecifyPickupDate)
                stored.PickupDate = model.PickupDate.Value;
            if (model.PickupAddress != null && _orderSettings.MerchandiseReturns_AllowToSpecifyPickupAddress)
                stored.PickupAddress = model.PickupAddress.ToEntity();
        });
    }
}
