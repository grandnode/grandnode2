using Grand.Domain.Configuration;

namespace Grand.Domain.Shipping;

public class ShippingProviderSettings : ISettings
{
    /// <summary>
    ///     Gets or sets system names of active Shipping rate  methods
    /// </summary>
    public List<string> ActiveSystemNames { get; set; } = new();
}