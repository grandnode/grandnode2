using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Utilities.Checkout;
using Grand.Domain.Common;
using Grand.Domain.Orders;
using Grand.Mediator;
using Grand.Module.Api.Commands.Models.Orders;
using Grand.Module.Api.DTOs.Customers;
using Grand.Module.Api.Extensions;

namespace Grand.Module.Api.Commands.Handlers.Orders;

public class UpdateMerchandiseReturnCommandHandler : IRequestHandler<UpdateMerchandiseReturnCommand, string?>
{
    private readonly IMerchandiseReturnService _merchandiseReturnService;
    private readonly IMerchandiseReturnWorkflowService _merchandiseReturnWorkflowService;
    private readonly ISettingService _settingService;

    public UpdateMerchandiseReturnCommandHandler(IMerchandiseReturnWorkflowService merchandiseReturnWorkflowService,
        IMerchandiseReturnService merchandiseReturnService, ISettingService settingService)
    {
        _merchandiseReturnWorkflowService = merchandiseReturnWorkflowService;
        _merchandiseReturnService = merchandiseReturnService;
        _settingService = settingService;
    }

    public async Task<string?> Handle(UpdateMerchandiseReturnCommand request, CancellationToken cancellationToken)
    {
        //a field the client did not send stays as stored: a PUT of the staff notes alone keeps the customer's comment
        //and the pickup address; an empty string clears a text, an address with no field filled in clears it
        var model = request.Model;
        var allowPickupDate = false;
        var allowPickupAddress = false;
        if (model.PickupDate.HasValue || model.PickupAddress != null)
        {
            //the pickup date and address change only where the return's store asks for them, as in the panel; a
            //change the store does not ask for is refused, and a value sent back as it is stored is not a change
            var merchandiseReturn = await _merchandiseReturnService.GetMerchandiseReturnById(request.Id);
            if (merchandiseReturn == null)
                return MerchandiseReturnErrors.NotFound;

            var orderSettings = await _settingService.LoadSetting<OrderSettings>(merchandiseReturn.StoreId);
            allowPickupDate = orderSettings.MerchandiseReturns_AllowToSpecifyPickupDate;
            allowPickupAddress = orderSettings.MerchandiseReturns_AllowToSpecifyPickupAddress;
            if ((!allowPickupDate && model.PickupDate.HasValue &&
                 model.PickupDate.Value != merchandiseReturn.PickupDate) ||
                (!allowPickupAddress && model.PickupAddress != null &&
                 !SameAddress(model.PickupAddress, merchandiseReturn.PickupAddress)))
                return MerchandiseReturnErrors.PickupNotAllowed;
        }

        return await _merchandiseReturnWorkflowService.Update(request.Id, stored =>
        {
            if (model.CustomerComments != null)
                stored.CustomerComments = model.CustomerComments;
            if (model.StaffNotes != null)
                stored.StaffNotes = model.StaffNotes;
            if (model.ExternalId != null)
                stored.ExternalId = model.ExternalId;
            if (model.PickupDate.HasValue && allowPickupDate)
                stored.PickupDate = model.PickupDate.Value;
            if (model.PickupAddress != null && allowPickupAddress)
                stored.PickupAddress = IsEmpty(model.PickupAddress) ? null : model.PickupAddress.ToEntity();
        });
    }

    private static bool SameAddress(AddressDto sent, Address? stored)
    {
        return stored == null
            ? IsEmpty(sent)
            : Fields(sent).SequenceEqual(Fields(stored), StringComparer.Ordinal);
    }

    private static bool IsEmpty(AddressDto address)
    {
        return Fields(address).All(x => x.Length == 0);
    }

    private static IEnumerable<string> Fields(AddressDto x)
    {
        return new[] {
            x.FirstName, x.LastName, x.Email, x.Company, x.VatNumber, x.CountryId, x.StateProvinceId, x.City,
            x.Address1, x.Address2, x.ZipPostalCode, x.PhoneNumber, x.FaxNumber, x.Note
        }.Select(f => f ?? "");
    }

    private static IEnumerable<string> Fields(Address x)
    {
        return new[] {
            x.FirstName, x.LastName, x.Email, x.Company, x.VatNumber, x.CountryId, x.StateProvinceId, x.City,
            x.Address1, x.Address2, x.ZipPostalCode, x.PhoneNumber, x.FaxNumber, x.Note
        }.Select(f => f ?? "");
    }
}
