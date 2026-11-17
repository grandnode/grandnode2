using Grand.Module.Api.DTOs.Customers;
using Grand.Module.Api.Models;

namespace Grand.Module.Api.DTOs.Orders;

public class MerchandiseReturnDto : BaseApiEntityModel
{
    public int ReturnNumber { get; set; }
    public string ExternalId { get; set; }
    public string StoreId { get; set; }
    public string OrderId { get; set; }
    public string OwnerId { get; set; }
    public string SeId { get; set; }
    public string CustomerId { get; set; }
    public string VendorId { get; set; }
    public string CustomerComments { get; set; }
    public string StaffNotes { get; set; }

    /// <summary>
    ///     The return status as its stored numeric value; the enum is computed, not stored
    /// </summary>
    public int MerchandiseReturnStatusId { get; set; }

    public DateTime? PickupDate { get; set; }
    public AddressDto PickupAddress { get; set; }
    public MerchandiseReturnShippingDto ReturnShipping { get; set; }
    public MerchandiseReturnRefundDto Refund { get; set; }
    public IList<MerchandiseReturnItemDto> MerchandiseReturnItems { get; set; } = new List<MerchandiseReturnItemDto>();
    public IList<MerchandiseReturnStatusChangeDto> StatusHistory { get; set; } = new List<MerchandiseReturnStatusChangeDto>();

    /// <summary>
    ///     The statuses the return may change to, computed from the current status; never mapped from the entity.
    ///     Received is offered only when the return may be marked received by hand (a return receipt was posted, or no
    ///     receipt can take its goods in); a list never offers it, read the return by id to know
    /// </summary>
    public IList<int> NextStatuses { get; set; } = new List<int>();
}

public class MerchandiseReturnItemDto
{
    public string Id { get; set; }
    public string OrderItemId { get; set; }
    public string ReasonId { get; set; }
    public string ReasonForReturn { get; set; }
    public int Quantity { get; set; }
    public string ActionId { get; set; }
    public string RequestedAction { get; set; }
}

public class MerchandiseReturnShippingDto
{
    public string ProviderSystemName { get; set; }
    public string OptionCode { get; set; }
    public string OptionName { get; set; }
    public double Cost { get; set; }
    public double CurrencyRate { get; set; }
    public ReturnDropOffPointDto DropOffPoint { get; set; }
    public AddressDto ReturnAddress { get; set; }
    public string TrackingNumber { get; set; }
    public string ReturnCode { get; set; }
    public string LabelDownloadId { get; set; }
    public string Instructions { get; set; }
    public int LabelStatusId { get; set; }
    public string LabelError { get; set; }
    public Dictionary<string, string> ProviderData { get; set; } = new();
}

public class MerchandiseReturnRefundDto
{
    public double ItemsAmount { get; set; }
    public double ReturnShippingCost { get; set; }
    public double Amount { get; set; }
    public bool Offline { get; set; }
    public string PaymentTransactionId { get; set; }
    public bool OutcomeUnknown { get; set; }
    public string RefundedBy { get; set; }
    public DateTime CreatedOnUtc { get; set; }
    public DateTime? RefundedOnUtc { get; set; }
}

public class MerchandiseReturnStatusChangeDto
{
    public int StatusId { get; set; }
    public DateTime CreatedOnUtc { get; set; }
}

public class ReturnDropOffPointDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public AddressDto Address { get; set; }
    public string OpeningHours { get; set; }
    public string WarehouseId { get; set; }
}

public class MerchandiseReturnListModel
{
    public IList<MerchandiseReturnDto> Items { get; set; } = new List<MerchandiseReturnDto>();
    public int TotalCount { get; set; }
}
