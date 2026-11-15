using Grand.Infrastructure.Validators;
using Grand.Module.Api.Models;

namespace Grand.Module.Api.DTOs.Shipping;

public class WarehouseDto : BaseApiEntityModel
{
    public string Name { get; set; }
    [NoHtml]
    public string AdminComment { get; set; }

    /// <summary>
    ///     The stock of this warehouse belongs to an external system (ERP/WMS); it changes only through the stock
    ///     integration API and the shop's own movements to and from customers
    /// </summary>
    public bool ExternallyManaged { get; set; }
}