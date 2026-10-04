namespace Grand.Web.AdminShared.Models.Orders;

/// <summary>What the shipment info form posts in one go. The per-field actions
/// (SetTrackingNumber, SetShipmentAdminComment, EditShippedDate, EditDeliveryDate) stay for
/// callers outside the panel.</summary>
public record ShipmentInfoModel(
    string Id,
    string TrackingNumber,
    string AdminComment,
    DateTime? ShippedDate,
    DateTime? DeliveryDate);
