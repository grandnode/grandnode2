namespace Grand.Module.Api.DTOs.Orders;

/// <summary>
///     The body of a status transition request: the status to change to, and the note the customer sees (sent on a
///     rejection)
/// </summary>
public class MerchandiseReturnStatusChangeRequest
{
    public int? Status { get; set; }
    public string? CustomerNote { get; set; }
}
