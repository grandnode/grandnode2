namespace Grand.Web.Vendor.Extensions;

public static class Constants
{
    public const string AreaVendor = "Vendor";
    public static string LayoutVendor => $"~/Areas/{AreaVendor}/Views/Shared/_VendorLayout.cshtml";
    public static string LayoutVendorLogin => $"~/Areas/{AreaVendor}/Views/Shared/_VendorLoginLayout.cshtml";
}