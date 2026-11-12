using Grand.Domain.Common;
using Grand.Domain.Configuration;

namespace Grand.Domain.Shipping;

public class ReturnShippingProviderSettings : ISettings
{
    /// <summary>
    ///     Gets or sets system names of active return shipping providers
    /// </summary>
    public List<string> ActiveSystemNames { get; set; } = new();

    /// <summary>
    ///     Gets or sets the address the returned parcels are sent to
    /// </summary>
    public Address ReturnAddress { get; set; }

    /// <summary>
    ///     Gets or sets the cost of a return the customer sends back on their own
    /// </summary>
    public double SendYourselfCost { get; set; }
}
